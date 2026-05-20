import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpServer;

import java.io.IOException;
import java.io.OutputStream;
import java.net.InetSocketAddress;
import java.nio.charset.StandardCharsets;
import java.time.LocalDate;

public class TicketIntegrationService {
    public static void main(String[] args) throws IOException {
        HttpServer server = HttpServer.create(new InetSocketAddress(7003), 0);
        server.createContext("/health", TicketIntegrationService::health);
        server.createContext("/api/java/tickets/qr", TicketIntegrationService::generateQr);
        server.createContext("/api/java/tickets/invoice-code", TicketIntegrationService::generateInvoiceCode);
        server.setExecutor(null);
        System.out.println("Java integration service running on http://localhost:7003");
        server.start();
    }

    private static void health(HttpExchange exchange) throws IOException {
        logRequest(exchange, "health");
        writeJson(exchange, 200, "{\"status\":\"ok\",\"service\":\"java-ticket-service\"}");
    }

    private static void generateQr(HttpExchange exchange) throws IOException {
        logRequest(exchange, "tickets/qr");
        if (!"POST".equalsIgnoreCase(exchange.getRequestMethod())) {
            writeJson(exchange, 405, "{\"message\":\"method not allowed\"}");
            return;
        }

        String body = readBody(exchange);
        String bookingId = extractNumber(body, "bookingId", "0");
        String tripId = extractNumber(body, "tripId", "0");
        String seats = extractStringArray(body, "seatLabels");
        String phone = extractString(body, "customerPhone", "");
        System.out.println("[Java] QR bookingId=" + bookingId + ", tripId=" + tripId + ", seats=" + seats);

        String qrText = "BOOKING:" + bookingId + ";TRIP:" + tripId + ";SEAT:" + seats + ";PHONE:" + phone;
        writeJson(exchange, 200, "{\"qrText\":\"" + escape(qrText) + "\"}");
    }

    private static void generateInvoiceCode(HttpExchange exchange) throws IOException {
        logRequest(exchange, "tickets/invoice-code");
        if (!"POST".equalsIgnoreCase(exchange.getRequestMethod())) {
            writeJson(exchange, 405, "{\"message\":\"method not allowed\"}");
            return;
        }

        String body = readBody(exchange);
        String bookingId = extractNumber(body, "bookingId", "0");
        System.out.println("[Java] invoice bookingId=" + bookingId);
        String digits = String.format("%06d", Integer.parseInt(bookingId));
        String code = "VXAZ-" + LocalDate.now().toString().replace("-", "") + "-" + digits;
        writeJson(exchange, 200, "{\"invoiceCode\":\"" + code + "\"}");
    }

    private static void logRequest(HttpExchange exchange, String action) {
        System.out.println("[Java] " + action + " " + exchange.getRequestMethod() + " " + exchange.getRequestURI());
    }

    private static String readBody(HttpExchange exchange) throws IOException {
        return new String(exchange.getRequestBody().readAllBytes(), StandardCharsets.UTF_8);
    }

    private static void writeJson(HttpExchange exchange, int statusCode, String body) throws IOException {
        byte[] bytes = body.getBytes(StandardCharsets.UTF_8);
        exchange.getResponseHeaders().set("Content-Type", "application/json; charset=utf-8");
        exchange.sendResponseHeaders(statusCode, bytes.length);
        try (OutputStream os = exchange.getResponseBody()) {
            os.write(bytes);
        }
    }

    private static String extractString(String body, String key, String fallback) {
        String marker = "\"" + key + "\"";
        int start = body.indexOf(marker);
        if (start < 0) return fallback;
        int colon = body.indexOf(':', start + marker.length());
        if (colon < 0) return fallback;
        int firstQuote = body.indexOf('"', colon + 1);
        if (firstQuote < 0) return fallback;
        int secondQuote = body.indexOf('"', firstQuote + 1);
        if (secondQuote < 0) return fallback;
        return body.substring(firstQuote + 1, secondQuote);
    }

    private static String extractNumber(String body, String key, String fallback) {
        String marker = "\"" + key + "\"";
        int start = body.indexOf(marker);
        if (start < 0) return fallback;
        int colon = body.indexOf(':', start + marker.length());
        if (colon < 0) return fallback;
        int index = colon + 1;
        while (index < body.length() && Character.isWhitespace(body.charAt(index))) index++;
        StringBuilder value = new StringBuilder();
        while (index < body.length() && Character.isDigit(body.charAt(index))) {
            value.append(body.charAt(index));
            index++;
        }
        return value.length() == 0 ? fallback : value.toString();
    }

    private static String extractStringArray(String body, String key) {
        String marker = "\"" + key + "\"";
        int start = body.indexOf(marker);
        if (start < 0) return "";
        int open = body.indexOf('[', start + marker.length());
        int close = body.indexOf(']', open + 1);
        if (open < 0 || close < 0) return "";
        return body.substring(open + 1, close)
            .replace("\"", "")
            .replace(" ", "");
    }

    private static String escape(String value) {
        return value.replace("\\", "\\\\").replace("\"", "\\\"");
    }
}
