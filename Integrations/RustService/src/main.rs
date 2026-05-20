use std::collections::HashSet;
use std::io::{Read, Write};
use std::net::{TcpListener, TcpStream};

fn main() {
    let listener = TcpListener::bind("127.0.0.1:7002").expect("cannot bind rust service");
    println!("Rust integration service running on http://localhost:7002");

    for stream in listener.incoming() {
        if let Ok(stream) = stream {
            handle_client(stream);
        }
    }
}

fn handle_client(mut stream: TcpStream) {
    let mut buffer = [0_u8; 8192];
    let bytes_read = match stream.read(&mut buffer) {
        Ok(size) => size,
        Err(_) => return,
    };

    let request = String::from_utf8_lossy(&buffer[..bytes_read]);
    let body = request.split("\r\n\r\n").nth(1).unwrap_or("");
    let first_line = request.lines().next().unwrap_or("");
    if !first_line.is_empty() {
        println!("[Rust] {}", first_line);
    }

    let (status, response_body) = if first_line.starts_with("GET /health") {
        ("200 OK", "{\"status\":\"ok\",\"service\":\"rust-seat-service\"}".to_string())
    } else if first_line.starts_with("POST /api/rust/seats/generate") {
        let capacity = extract_number(body, "capacity").unwrap_or(0);
        println!("[Rust] seats/generate capacity={}", capacity);
        let seats = generate_seat_labels(capacity);
        ("200 OK", format!("{{\"seats\":{}}}", json_string_array(&seats)))
    } else if first_line.starts_with("POST /api/rust/seats/validate") {
        let capacity = extract_number(body, "capacity").unwrap_or(0);
        let requested = extract_string_array(body, "seatLabels");
        println!("[Rust] seats/validate capacity={} requested={:?}", capacity, requested);
        let valid: HashSet<String> = generate_seat_labels(capacity).into_iter().collect();
        let invalid: Vec<String> = requested
            .into_iter()
            .map(|seat| seat.trim().to_uppercase())
            .filter(|seat| !valid.contains(seat))
            .collect();

        (
            "200 OK",
            format!(
                "{{\"isValid\":{},\"invalidSeats\":{}}}",
                if invalid.is_empty() { "true" } else { "false" },
                json_string_array(&invalid)
            ),
        )
    } else {
        ("404 Not Found", "{\"message\":\"not found\"}".to_string())
    };

    let response = format!(
        "HTTP/1.1 {}\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: {}\r\nConnection: close\r\n\r\n{}",
        status,
        response_body.as_bytes().len(),
        response_body
    );
    let _ = stream.write_all(response.as_bytes());
}

fn generate_seat_labels(capacity: i32) -> Vec<String> {
    let safe_capacity = capacity.max(0);
    let mut labels = Vec::new();

    for index in 0..safe_capacity {
        let row_index = index / 4;
        let seat_number = index % 4 + 1;
        labels.push(format!("{}{}", row_label(row_index), seat_number));
    }

    labels
}

fn row_label(mut row_index: i32) -> String {
    let mut label = String::new();
    loop {
        let ch = (b'A' + (row_index % 26) as u8) as char;
        label.insert(0, ch);
        row_index = row_index / 26 - 1;
        if row_index < 0 {
            break;
        }
    }
    label
}

fn extract_number(body: &str, key: &str) -> Option<i32> {
    let marker = format!("\"{}\"", key);
    let start = body.find(&marker)?;
    let after_key = &body[start + marker.len()..];
    let colon = after_key.find(':')?;
    let number_part = after_key[colon + 1..]
        .chars()
        .skip_while(|c| c.is_whitespace())
        .take_while(|c| c.is_ascii_digit() || *c == '-')
        .collect::<String>();
    number_part.parse::<i32>().ok()
}

fn extract_string_array(body: &str, key: &str) -> Vec<String> {
    let marker = format!("\"{}\"", key);
    let Some(start) = body.find(&marker) else {
        return Vec::new();
    };
    let after_key = &body[start + marker.len()..];
    let Some(open) = after_key.find('[') else {
        return Vec::new();
    };
    let after_open = &after_key[open + 1..];
    let Some(close) = after_open.find(']') else {
        return Vec::new();
    };

    after_open[..close]
        .split(',')
        .map(|item| item.trim().trim_matches('"').to_string())
        .filter(|item| !item.is_empty())
        .collect()
}

fn json_string_array(items: &[String]) -> String {
    let values = items
        .iter()
        .map(|item| format!("\"{}\"", item.replace('\\', "\\\\").replace('"', "\\\"")))
        .collect::<Vec<String>>()
        .join(",");
    format!("[{}]", values)
}
