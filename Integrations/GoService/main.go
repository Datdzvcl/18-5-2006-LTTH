package main

import (
	"encoding/json"
	"log"
	"math"
	"net/http"
	"time"
)

type StopsRequest struct {
	DepartureLocation string `json:"departureLocation"`
	ArrivalLocation   string `json:"arrivalLocation"`
	DepartureTime     string `json:"departureTime"`
	ArrivalTime       string `json:"arrivalTime"`
}

type StopResponse struct {
	StopName      string `json:"stopName"`
	StopAddress   string `json:"stopAddress"`
	StopOrder     int    `json:"stopOrder"`
	StopType      int    `json:"stopType"`
	ArrivalOffset int    `json:"arrivalOffset"`
}

type RouteEstimateRequest struct {
	From string `json:"from"`
	To   string `json:"to"`
}

type RouteEstimateResponse struct {
	RouteName        string `json:"routeName"`
	DistanceKm       int    `json:"distanceKm"`
	EstimatedMinutes int    `json:"estimatedMinutes"`
}

func main() {
	http.HandleFunc("/api/go/stops/generate", generateStops)
	http.HandleFunc("/api/go/routes/estimate", estimateRoute)
	http.HandleFunc("/health", func(w http.ResponseWriter, r *http.Request) {
		logRequest(r, "health")
		writeJSON(w, map[string]string{"status": "ok", "service": "go-route-service"})
	})

	log.Println("Go integration service running on http://localhost:7001")
	log.Fatal(http.ListenAndServe(":7001", nil))
}

func generateStops(w http.ResponseWriter, r *http.Request) {
	logRequest(r, "stops/generate")
	if r.Method != http.MethodPost {
		http.Error(w, "method not allowed", http.StatusMethodNotAllowed)
		return
	}

	var req StopsRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "invalid json", http.StatusBadRequest)
		return
	}
	log.Printf("[Go] stops/generate route=%s -> %s", req.DepartureLocation, req.ArrivalLocation)

	totalMinutes := estimateMinutesFromTimes(req.DepartureTime, req.ArrivalTime)
	middleOffset := totalMinutes / 2
	if totalMinutes > 0 && middleOffset == 0 {
		middleOffset = 1
	}

	stops := []StopResponse{
		{
			StopName:      "Bến xe " + req.DepartureLocation,
			StopAddress:   "Trung tâm " + req.DepartureLocation,
			StopOrder:     1,
			StopType:      1,
			ArrivalOffset: 0,
		},
		{
			StopName:      "Trạm dừng giữa tuyến " + req.DepartureLocation + " - " + req.ArrivalLocation,
			StopAddress:   "Quốc lộ chính tuyến " + req.DepartureLocation + " - " + req.ArrivalLocation,
			StopOrder:     2,
			StopType:      3,
			ArrivalOffset: middleOffset,
		},
		{
			StopName:      "Bến xe " + req.ArrivalLocation,
			StopAddress:   "Trung tâm " + req.ArrivalLocation,
			StopOrder:     3,
			StopType:      2,
			ArrivalOffset: totalMinutes,
		},
	}

	writeJSON(w, map[string]any{"items": stops})
}

func estimateRoute(w http.ResponseWriter, r *http.Request) {
	logRequest(r, "routes/estimate")
	if r.Method != http.MethodPost {
		http.Error(w, "method not allowed", http.StatusMethodNotAllowed)
		return
	}

	var req RouteEstimateRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "invalid json", http.StatusBadRequest)
		return
	}
	log.Printf("[Go] routes/estimate route=%s -> %s", req.From, req.To)

	distance := estimateDistance(req.From, req.To)
	writeJSON(w, RouteEstimateResponse{
		RouteName:        req.From + " - " + req.To,
		DistanceKm:       distance,
		EstimatedMinutes: int(math.Round(float64(distance) / 55.0 * 60.0)),
	})
}

func logRequest(r *http.Request, action string) {
	log.Printf("[Go] %s %s %s from %s", action, r.Method, r.URL.Path, r.RemoteAddr)
}

func estimateMinutesFromTimes(departure string, arrival string) int {
	departureTime, depErr := time.Parse(time.RFC3339Nano, departure)
	arrivalTime, arrErr := time.Parse(time.RFC3339Nano, arrival)
	if depErr != nil || arrErr != nil {
		departureTime, depErr = time.Parse("2006-01-02T15:04:05", departure)
		arrivalTime, arrErr = time.Parse("2006-01-02T15:04:05", arrival)
	}
	if depErr != nil || arrErr != nil || !arrivalTime.After(departureTime) {
		return 0
	}
	return int(arrivalTime.Sub(departureTime).Minutes())
}

func estimateDistance(from string, to string) int {
	key := from + "|" + to
	distances := map[string]int{
		"Hà Nội|Hải Phòng": 120,
		"Hải Phòng|Hà Nội": 120,
		"Hà Nội|Đà Nẵng":   760,
		"Đà Nẵng|Hà Nội":   760,
		"Đà Nẵng|Huế":      100,
		"Huế|Đà Nẵng":      100,
	}
	if value, ok := distances[key]; ok {
		return value
	}
	return 250
}

func writeJSON(w http.ResponseWriter, value any) {
	w.Header().Set("Content-Type", "application/json; charset=utf-8")
	_ = json.NewEncoder(w).Encode(value)
}
