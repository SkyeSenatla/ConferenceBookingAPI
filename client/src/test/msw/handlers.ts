import { http, HttpResponse } from "msw";
const API = "http://localhost:5062";


export const handlers = [
  http.post(`${API}/api/bookings`, () => {
    return HttpResponse.json(
      {
        id: "test-booking-id",
        title: "Q3 Planning",
        type: "Meeting",
        roomName: "Board Room",
        floor: "Floor 1",
        startTime: "2025-12-01T09:00:00Z",
        endTime: "2025-12-01T10:00:00Z",
        organizerEmail: "test@company.com",
        attendeeCount: 0,
        externalAttendees: [],
      },
      { status: 201 }
    );
  }),
  http.get(`${API}/api/rooms`, () => {
    return HttpResponse.json([]);
  }),
];