// Mirrors the backend's BookingType enum.
// The API serialises enums as strings — "Meeting", not 0.
export type BookingType =
  | "Meeting"
  | "ClientPresentation"
  | "Training"
  | "Maintenance"
  | "TeamEvent";

// Mirrors RoomResponse.cs from the Conference Booking API.
export interface RoomResponse {
  id: string;        // Guid serialised as lowercase hyphenated string
  name: string;
  floor: string;
  capacity: number;
  isAvailable: boolean;
}

// Mirrors BookingResponse.cs from the Conference Booking API.
export interface BookingResponse {
  id: string;
  title: string;
  type: BookingType;
  roomName: string;
  floor: string;
  startTime: string;  // ISO 8601 — e.g. "2025-06-19T09:00:00Z"
  endTime: string;
  organizerEmail: string;
  attendeeCount: number;
  externalAttendees: string[];
}
