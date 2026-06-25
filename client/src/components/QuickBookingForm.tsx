"use client";

import { useActionState } from "react";
import { quickCreateBooking } from "@/app/actions/createBooking";


export function QuickBookingForm({ roomId }: { roomId: string }) {
    // useActionState returns [currentState, wrappedAction, isPendingFlag]. 
    // The second argument is the initial state — null before anything is submitted. 
    const [state, formAction, isPending] = useActionState(
        quickCreateBooking,
        null
    );

    // When the action returns success, replace the form with a confirmation. 
    // The form is gone — submitting again requires navigating away and back. 
    if (state?.status === "success") {
        return (
            <div className="rounded-lg border border-green-200 bg-green-50 p-4 dark:border
green-800 dark:bg-green-950">
                <p className="text-sm font-medium text-green-800 dark:text-green-300">
                    {state.message}
                </p>
            </div>
        );
    }

    return (
        <div className="rounded-lg border border-gray-200 bg-white p-6 shadow-sm 
dark:border-gray-700 dark:bg-gray-800">
            <h2 className="mb-4 text-lg font-semibold text-gray-900 dark:text-gray-100">
                Quick Book
            </h2>

            {/* Error returned by the Server Action — displayed above the fields */}
            {state?.status === "error" && (
                <div className="mb-4 rounded-md border border-red-200 bg-red-50 p-3 
dark:border-red-800 dark:bg-red-950">
                    <p className="text-sm text-red-700 dark:text-red-400">
                        {state.message}
                    </p>
                </div>
            )}

            {/* 
        action={formAction} — not action={quickCreateBooking} directly. 
        useActionState wraps the action to thread the state through. 
        Always use the formAction from the hook, not the raw action. 
      */}
            <form action={formAction} className="space-y-4">
                {/* roomId travels as a hidden field inside the FormData */}
                <input type="hidden" name="roomId" value={roomId} />

                <div>
                    <label
                        htmlFor="qs-title"
                        className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300"
                    >
                        Meeting title
                    </label>
                    <input
                        id="qs-title"
                        name="title"
                        type="text"
                        required
                        placeholder="e.g. Team standup"
                        className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm 
focus:outline-none focus:ring-2 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray
900 dark:text-gray-100"
                    />
                </div>

                <div>
                    <label
                        htmlFor="qs-email"
                        className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300"
                    >
                        Your email
                    </label>
                    <input
                        id="qs-email"
                        name="organizerEmail"
                        type="email"
                        required
                        placeholder="you@company.com"
                        className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm 
focus:outline-none focus:ring-2 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray
900 dark:text-gray-100"
                    />
                </div>

                <p className="text-xs text-gray-400 dark:text-gray-500">
                    Books 1 hour from now for 1 hour.
                </p>

                <button
                    type="submit"
                    disabled={isPending}
                    className="w-full rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text
white hover:bg-blue-700 disabled:cursor-not-allowed disabled:bg-blue-400"
                >
                    {isPending ? "Booking…" : "Book Now"}
                </button>
            </form>
        </div>
    );
}