import { describe, it, expect } from "vitest";
import { screen, fireEvent } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { server } from "./msw/server";
import { renderWithProviders } from "./utils";
import { BookingForm } from "@/components/BookingForm";


const PROPS = { roomId: "test-room", roomName: "Board Room" };
async function fillForm() {
  const user = userEvent.setup();
  await user.type(screen.getByLabelText("Title"), "Q3 Planning");
  fireEvent.change(screen.getByLabelText("Start time"), {
    target: { value: "2025-12-01T09:00" },
  });
  fireEvent.change(screen.getByLabelText("End time"), {
    target: { value: "2025-12-01T10:00" },
  });
  await user.type(screen.getByLabelText("Organiser email"), "test@company.com");
}
describe("BookingForm", () => {
  it("shows validation errors when submitted empty", async () => {
    const user = userEvent.setup();
      renderWithProviders(<BookingForm {...PROPS} />);
    await user.click(screen.getByRole("button", { name: "Book Room" }));
    expect(screen.getByText("Title is required")).toBeInTheDocument();
    expect(screen.getByText("Start time is required")).toBeInTheDocument();
    expect(screen.getByText("Organiser email is required")).toBeInTheDocument();
  });
  it("resets the form after a successful submission", async () => {
    renderWithProviders(<BookingForm {...PROPS} />);
    await fillForm();
    await userEvent.setup().click(
      screen.getByRole("button", { name: "Book Room" })
    );
    // findByRole waits -- button shows "Booking..." while POST is in flight,
    // then returns to "Book Room" once onSuccess fires
    await screen.findByRole("button", { name: "Book Room" });
    // reset() was called in onSuccess -- title field is empty again
    expect(screen.getByLabelText("Title")).toHaveValue("");
  });
  it("retains form values when the API returns an error", async () => {
    server.use(
      http.post("http://localhost:5062/api/bookings", () => {
        return new HttpResponse(null, { status: 500 });
      })
    );
    renderWithProviders(<BookingForm {...PROPS} />);
    await fillForm();
    await userEvent.setup().click(
      screen.getByRole("button", { name: "Book Room" })
    );
    await screen.findByRole("button", { name: "Book Room" });
    // onError does not call reset() -- values stay so user can correct and retry
    expect(screen.getByLabelText("Title")).toHaveValue("Q3 Planning");
  });
})