import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { renderWithProviders } from "./utils";
import { BookingWizard } from "@/components/BookingWizard";

const PROPS = { roomId: "test-room", roomName: "Board Room" };

describe("BookingWizard", () => {
  it("renders the Meeting Details heading on mount", () => {
    renderWithProviders(<BookingWizard {...PROPS} />);
    expect(
      screen.getByRole("heading", { name: "Meeting Details" })
    ).toBeInTheDocument();
  });

  it("blocks advancement when required fields are empty", async () => {
     const user = userEvent.setup();
    renderWithProviders(<BookingWizard {...PROPS} />);
    await user.click(screen.getByRole("button", { name: "Next" }));
    expect(screen.getByText("Title is required")).toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: "Meeting Details" })
    ).toBeInTheDocument();
  });

  it("advances to the Schedule step when step 1 is complete", async () => {
    const user = userEvent.setup();
    renderWithProviders(<BookingWizard {...PROPS} />);
    await user.type(screen.getByLabelText("Title"), "Q3 Planning");
    await user.click(screen.getByRole("button", { name: "Next" }));
    expect(
      screen.getByRole("heading", { name: "Schedule" })
    ).toBeInTheDocument();
  });
  
  it("preserves step 1 values after navigating back from step 2", async () => {
    const user = userEvent.setup();
    renderWithProviders(<BookingWizard {...PROPS} />);
    await user.type(screen.getByLabelText("Title"), "Q3 Planning");
    await user.click(screen.getByRole("button", { name: "Next" }));
    await user.click(screen.getByRole("button", { name: "Back" }));
    expect(screen.getByDisplayValue("Q3 Planning")).toBeInTheDocument();
  });
});