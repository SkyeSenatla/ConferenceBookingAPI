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

// Mirrors PagedResponse<T>.cs — the pagination envelope returned by GET /api/bookings.
export interface PagedResponse<T> {
  data: T[];
  page: number;
  pageSize: number;
  totalPages: number;
  totalCount: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// Mirrors UserResponse.cs — returned by GET /api/users (Admin only).
export interface UserResponse {
  username: string;
  role: string;
}
// Mirrors CreateBookingRequest.cs — sent as the body of POST /api/bookings. 
// Guid → string, DateTime? → string (ISO 8601 enforced by Zod). 
export interface CreateBookingRequest { 
title: string; 
roomId: string; 
startTime: string; 
endTime: string; 
type: BookingType; 
organizerEmail: string; 
description?: string; 
}

