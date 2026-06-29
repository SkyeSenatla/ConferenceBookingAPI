"use client";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useMutation, useQueryClient, useQuery } from "@tanstack/react-query";
import { createBooking } from "@/lib/api";
import { toast } from "sonner";
import { cn } from "@/lib/utils";
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, AlertDialogTrigger } from "@/components/ui/alert-dialog";
import { useDebouncedCallback } from "use-debounce";

const wizardSchema = z
    .object({
        title: z.string().min(1, "Title is required"),
        type: z.enum(
            ["Meeting", "ClientPresentation", "Training", "Maintenance", "TeamEvent"] as const,
            { message: "Please select a type" }
        ),
        description: z.string().optional(),
        startTime: z.string().min(1, "Start time is required"),
        endTime: z.string().min(1, "End time is required"),
        organizerEmail: z
            .string()
            .min(1, "Organiser email is required")
            .email("Must be a valid email"),
    })
    .refine((d) => new Date(d.endTime) > new Date(d.startTime), {
        message: "End time must be after start time",
        path: ["endTime"],
    });
type WizardData = z.infer<typeof wizardSchema>;

// Mock — in production, replace with a real API call 
async function searchAttendees(query: string) {
    await new Promise((r) => setTimeout(r, 150));
    const MOCK = [
        { email: "alice@company.com", name: "Alice Smith" },
        { email: "bob@company.com", name: "Bob Jones" },
        { email: "carol@company.com", name: "Carol White" },
        { email: "david@company.com", name: "David Kim" },
    ];
    return MOCK.filter(
        (u) =>
            u.email.includes(query.toLowerCase()) ||
            u.name.toLowerCase().includes(query.toLowerCase())
    );
}

type Step = "details" | "schedule" | "confirm";
const STEPS: Step[] = ["details", "schedule", "confirm"];
const STEP_LABELS: Record<Step, string> = {
    details: "Meeting Details",
    schedule: "Schedule",
    confirm: "Review & Confirm",
};
const STEP_FIELDS: Record<Step, (keyof WizardData)[]> = {
    details: ["title", "type", "description"],
    schedule: ["startTime", "endTime", "organizerEmail"],
    confirm: [],
};
interface BookingWizardProps { roomId: string; roomName: string; }

export function BookingWizard({ roomId, roomName }: BookingWizardProps) {
    const [currentStep, setCurrentStep] = useState<Step>("details");
    const queryClient = useQueryClient();
    const form = useForm<WizardData>({
        resolver: zodResolver(wizardSchema),
        defaultValues: { type: "Meeting" },
    });
    const { register, handleSubmit, trigger, getValues, formState: { errors } } = form;

    const createBookingMutation = useMutation({
        mutationFn: createBooking,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ["rooms"] });
            form.reset();
            setCurrentStep("details");
            toast.success("Room booked!", { description: `${roomName} has been reserved.` });
        },
        onError: (error: Error) => {
            toast.error("Booking failed", { description: error.message });
        },
    });

    const stepIndex = STEPS.indexOf(currentStep);

    const [attendeeSearch, setAttendeeSearch] = useState("");
    const [debouncedSearch, setDebouncedSearch] = useState("");
    const updateDebouncedSearch = useDebouncedCallback((val: string) => {
        setDebouncedSearch(val);
    }, 400);
    const { data: attendeeSuggestions, isFetching: isSearching } = useQuery({
        queryKey: ["attendees", debouncedSearch],
        queryFn: () => searchAttendees(debouncedSearch),
        enabled: debouncedSearch.length >= 2,
        staleTime: 30_000,
    });

    const goNext = async () => {
        const valid = await trigger(STEP_FIELDS[currentStep]);
        if (!valid) return;
        setCurrentStep(STEPS[stepIndex + 1]);
    };

    const goBack = () => setCurrentStep(STEPS[stepIndex - 1]);

    const onSubmit = async (data: WizardData) => {
        await createBookingMutation.mutateAsync({
            ...data,
            roomId,
            startTime: new Date(data.startTime).toISOString(),
            endTime: new Date(data.endTime).toISOString(),
        });
    };

    const inputBase =
        "w-full rounded-md border px-3 py-2 text-sm bg-white text-gray-900 " +
        "dark:bg-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500";

    return (
        <div className="rounded-lg border border-gray-200 bg-white p-6 shadow-sm dark:border-gray-700 dark:bg-gray-800">

            {/* Progress bar */}
            <div className="mb-6 flex gap-2">
                {STEPS.map((step, i) => (
                    <div
                        key={step}
                        className={cn(
                            "h-1.5 flex-1 rounded-full transition-colors",
                            i <= stepIndex ? "bg-blue-500" : "bg-gray-200 dark:bg-gray-700"
                        )}
                    />
                ))}
            </div>

            <h2 className="mb-0.5 text-lg font-semibold text-gray-900 dark:text-gray-100">
                {STEP_LABELS[currentStep]}
            </h2>
            <p className="mb-4 text-xs text-gray-400 dark:text-gray-500">
                Step {stepIndex + 1} of {STEPS.length} · Booking {roomName}
            </p>

            <form onSubmit={handleSubmit(onSubmit)} noValidate>

                {/* Step 1: Details */}
                {currentStep === "details" && (
                    <div className="space-y-4">
                        <div>
                            <label htmlFor="wiz-title" className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Title</label>
                            <input id="wiz-title" type="text" {...register("title")}
                                className={cn(inputBase, errors.title ? "border-red-500" : "border-gray-300 dark:border-gray-600")}
                                placeholder="e.g. Q3 Planning" />
                            {errors.title && <p className="mt-1 text-xs text-red-600">{errors.title.message}</p>}
                        </div>

                        <div>
                            <label htmlFor="wiz-type" className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Type</label>
                            <select id="wiz-type" {...register("type")}
                                className={cn(inputBase, errors.type ? "border-red-500" : "border-gray-300 dark:border-gray-600")}>
                                <option value="Meeting">Meeting</option>
                                <option value="ClientPresentation">Client Presentation</option>
                                <option value="Training">Training</option>
                                <option value="Maintenance">Maintenance</option>
                                <option value="TeamEvent">Team Event</option>
                            </select>
                        </div>

                        <div>
                            <label htmlFor="wiz-desc" className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
                                Description <span className="font-normal text-gray-400">(optional)</span>
                            </label>
                            <textarea id="wiz-desc" rows={3} {...register("description")}
                                className={cn(inputBase, "resize-none border-gray-300 dark:border-gray-600")}
                                placeholder="Agenda, requirements…" />
                        </div>
                    </div>
                )}

                {/* Step 2: Schedule */}
                {currentStep === "schedule" && (
                    <div className="space-y-4">
                        <div className="grid grid-cols-2 gap-3">
                            <div>
                                <label htmlFor="wiz-start" className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Start time</label>
                                <input id="wiz-start" type="datetime-local" {...register("startTime")}
                                    className={cn(inputBase, errors.startTime ? "border-red-500" : "border-gray-300 dark:border-gray-600")} />
                                {errors.startTime && <p className="mt-1 text-xs text-red-600">{errors.startTime.message}</p>}
                            </div>
                            <div>
                                <label htmlFor="wiz-end" className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">End time</label>
                                <input id="wiz-end" type="datetime-local" {...register("endTime")}
                                    className={cn(inputBase, errors.endTime ? "border-red-500" : "border-gray-300 dark:border-gray-600")} />
                                {errors.endTime && <p className="mt-1 text-xs text-red-600">{errors.endTime.message}</p>}
                            </div>
                        </div>

                        <div>
                            <label htmlFor="wiz-email" className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Organiser email</label>
                            <input id="wiz-email" type="email" {...register("organizerEmail")}
                                className={cn(inputBase, errors.organizerEmail ? "border-red-500" : "border-gray-300 dark:border-gray-600")}
                                placeholder="you@company.com" />
                            {errors.organizerEmail && <p className="mt-1 text-xs text-red-600">{errors.organizerEmail.message}</p>}
                        </div>
                        <div>
                            <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray
300">
                                Add attendees{" "}
                                <span className="font-normal text-gray-400">(optional — type 2+
                                    characters)</span>
                            </label>
                            <input
                                type="text"
                                value={attendeeSearch}
                                onChange={(e) => {
                                    setAttendeeSearch(e.target.value);
                                    updateDebouncedSearch(e.target.value);
                                }}
                                className={cn(inputBase, "border-gray-300 dark:border-gray-600")}
                                placeholder="Search by name or email…"
                            />

                            {isSearching && (
                                <p className="mt-1 text-xs text-gray-400">Searching…</p>
                            )}

                            {attendeeSuggestions && attendeeSuggestions.length > 0 && (
                                <ul className="mt-1 overflow-hidden rounded-md border border-gray-200 
dark:border-gray-700 divide-y divide-gray-100 dark:divide-gray-700">
                                    {attendeeSuggestions.map((u) => (
                                        <li
                                            key={u.email}
                                            className="cursor-pointer px-3 py-2 text-sm text-gray-700 dark:text-gray-300 
hover:bg-gray-50 dark:hover:bg-gray-700"
                                        >
                                            {u.name}{" "}
                                            <span className="text-gray-400">— {u.email}</span>
                                        </li>
                                    ))}
                                </ul>
                            )}

                            {debouncedSearch.length >= 2 && !isSearching && attendeeSuggestions?.length === 0
                                && (
                                    <p className="mt-1 text-xs text-gray-400">
                                        No attendees found for "{debouncedSearch}"
                                    </p>
                                )}
                        </div>
                    </div>
                )}

                {/* Step 3: Confirm */}
                {currentStep === "confirm" && (
                    <div>
                        <p className="mb-4 text-sm text-gray-500 dark:text-gray-400">Review before confirming.</p>
                        <div className="overflow-hidden rounded-md border border-gray-200 dark:border-gray-600 divide-y divide-gray-100 dark:divide-gray-700">
                            {[
                                { label: "Room", value: roomName },
                                { label: "Title", value: getValues("title") },
                                { label: "Type", value: getValues("type") },
                                { label: "Start", value: new Date(getValues("startTime")).toLocaleString() },
                                { label: "End", value: new Date(getValues("endTime")).toLocaleString() },
                                { label: "Organiser", value: getValues("organizerEmail") },
                            ].map(({ label, value }) => (
                                <div key={label} className="flex bg-gray-50 dark:bg-gray-700 px-4 py-2.5">
                                    <span className="w-24 shrink-0 text-xs font-semibold uppercase tracking-wide text-gray-400">{label}</span>
                                    <span className="text-sm text-gray-900 dark:text-gray-100">{value}</span>
                                </div>
                            ))}
                        </div>
                    </div>
                )}

                {/* Navigation */}
                <div className="mt-6 flex gap-3">
                    {stepIndex > 0 && (
                        <button type="button" onClick={goBack}
                            className="rounded-md border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-700">
                            Back
                        </button>
                    )}
                    {currentStep !== "confirm" ? (
                        <button type="button" onClick={goNext}
                            className="flex-1 rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700">
                            Next
                        </button>
                    ) : (
                        <AlertDialog>
                            <AlertDialogTrigger asChild>
                                <button
                                    type="button"
                                    disabled={createBookingMutation.isPending}
                                    className="flex-1 rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text
white hover:bg-blue-700 disabled:cursor-not-allowed disabled:bg-blue-400"
                                >
                                    {createBookingMutation.isPending ? "Booking…" : "Confirm Booking"}
                                </button>
                            </AlertDialogTrigger>
                            <AlertDialogContent>
                                <AlertDialogHeader>
                                    <AlertDialogTitle>Confirm this booking?</AlertDialogTitle>
                                    <AlertDialogDescription>
                                        You are about to book <strong>{roomName}</strong> for{" "}
                                        <strong>{getValues("title")}</strong>. This cannot be undone.
                                    </AlertDialogDescription>
                                </AlertDialogHeader>
                                <AlertDialogFooter>
                                    <AlertDialogCancel>Cancel</AlertDialogCancel>
                                    <AlertDialogAction onClick={handleSubmit(onSubmit)}>
                                        Yes, Book It
                                    </AlertDialogAction>
                                </AlertDialogFooter>
                            </AlertDialogContent>
                        </AlertDialog>
                    )}
                </div>

            </form>
        </div>
    );
}
