"use client"; 
import { useForm } from "react-hook-form"; 
import { zodResolver } from "@hookform/resolvers/zod"; 
import { z } from "zod"; 
import { useMutation, useQueryClient } from "@tanstack/react-query"; 
import { createBooking } from "@/lib/api"; 
import { cn } from "@/lib/utils"; 


// ── 1. Zod schema 
//───────────────────────────────────────────────────────────── 
// Each field mirrors a field on CreateBookingRequest. 
// .refine() adds a cross-field rule that cannot be expressed per-field. 
const bookingSchema = z 
.object({ 
title: z.string().min(1, "Title is required"), 
type: z.enum(
["Meeting", "ClientPresentation", "Training", "Maintenance", "TeamEvent"] as const,
{ message: "Please select a booking type" }
), 
startTime: z.string().min(1, "Start time is required"), 
endTime: z.string().min(1, "End time is required"), 
organizerEmail: z 
.string() 
.min(1, "Organiser email is required") 
.email("Must be a valid email address"), 
description: z.string().optional(), 
}) 
.refine((data) => new Date(data.endTime) > new Date(data.startTime), { 
message: "End time must be after start time", 
path: ["endTime"], // attach the error to the endTime field 
}); 
// ── 2. Infer the TypeScript type from the schema ────────────────────────────── 
// This is the single source of truth — the type and the validation rule 
// are defined once and stay in sync automatically. 
type BookingFormData = z.infer<typeof bookingSchema>; 

// ── 3. Props 
//────────────────────────────────────────────────────────────────── 
interface BookingFormProps { 
roomId: string; 
roomName: string; 
} 
// ── 4. Component 
//────────────────────────────────────────────────────────────── 
export function BookingForm({ roomId, roomName }: BookingFormProps) { 
const queryClient = useQueryClient(); 
// React Hook Form — zodResolver connects Zod to RHF's error map. 
const { 
register, 
handleSubmit, 
reset, 
    formState: { errors, isSubmitting }, 
  } = useForm<BookingFormData>({ 
    resolver: zodResolver(bookingSchema), 
    defaultValues: { type: "Meeting" }, 
  }); 
 
  // TanStack Query mutation — tracks network state separately from form state. 
  const createBookingMutation = useMutation({ 
    mutationFn: createBooking, 
    onSuccess: () => { 
      // Invalidate the rooms cache so availability badges update. 
      queryClient.invalidateQueries({ queryKey: ["rooms"] }); 
      reset(); 
    }, 
  }); 
 
  // Combined busy flag — true while Zod is validating OR while the POST is in flight. 
  const isBusy = isSubmitting || createBookingMutation.isPending; 
 
  // handleSubmit runs Zod first. onSubmit only fires when ALL fields are valid. 
  const onSubmit = async (data: BookingFormData) => { 
    await createBookingMutation.mutateAsync({ 
      ...data, 
      roomId, 
      // datetime-local gives "2025-06-19T09:00" — convert to full ISO so the 
      // backend receives an unambiguous UTC timestamp. 
      startTime: new Date(data.startTime).toISOString(), 
      endTime: new Date(data.endTime).toISOString(), 
}); 
};
// ── Base input class reused across all fields ──────────────────────────── 
const inputBase = 
"w-full rounded-md border px-3 py-2 text-sm " + 
"bg-white text-gray-900 " + 
"dark:bg-gray-900 dark:text-gray-100 " + 
"focus:outline-none focus:ring-2 focus:ring-blue-500"; 
return ( 
<div className="rounded-lg border border-gray-200 bg-white p-6 shadow-sm 
dark:border-gray-700 dark:bg-gray-800"> 
<h2 className="mb-4 text-lg font-semibold text-gray-900 dark:text-gray-100"> 
Book {roomName} 
</h2> 
{/* ── Form-level error banner — comes from the API, not from Zod ── */} 
{createBookingMutation.isError && ( 
        <div className="mb-4 rounded-md border border-red-200 bg-red-50 p-3 
dark:border-red-800 dark:bg-red-950"> 
          <p className="text-sm text-red-700 dark:text-red-400"> 
            {createBookingMutation.error.message} 
          </p> 
        </div> 
      )} 
 
      {/* ── Success banner ── */} 
      {createBookingMutation.isSuccess && ( 
        <div className="mb-4 rounded-md border border-green-200 bg-green-50 p-3 
dark:border-green-800 dark:bg-green-950"> 
          <p className="text-sm text-green-700 dark:text-green-400"> 
            Booking created successfully! 
          </p> 
        </div> 
      )} 
 
      <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4"> 
 
        {/* ── Title ── */} 
        <div> 
          <label 
            htmlFor="title" 
            className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300" 
          > 
            Title 
          </label> 
          <input 
            id="title" 
            type="text" 
            {...register("title")} 
            aria-invalid={!!errors.title} 
            className={cn( 
              inputBase, 
              errors.title 
                ? "border-red-500 dark:border-red-500" 
                : "border-gray-300 dark:border-gray-600" 
            )} 
            placeholder="e.g. Q3 Planning" 
          /> 
          {errors.title && ( 
            <p className="mt-1 text-xs text-red-600 dark:text-red-400"> 
              {errors.title.message} 
            </p> 
          )} 
        </div> 
 
        {/* ── Booking Type ── */} 
        <div> 
          <label 
            htmlFor="type" 
            className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300" 
          > 
            Type 
          </label> 
          <select 
            id="type" 
            {...register("type")} 
            aria-invalid={!!errors.type} 
            className={cn( 
              inputBase, 
              errors.type 
                ? "border-red-500 dark:border-red-500" 
                : "border-gray-300 dark:border-gray-600" 
            )} 
          > 
            <option value="Meeting">Meeting</option> 
            <option value="ClientPresentation">Client Presentation</option> 
            <option value="Training">Training</option> 
            <option value="Maintenance">Maintenance</option> 
            <option value="TeamEvent">Team Event</option> 
          </select> 
          {errors.type && ( 
            <p className="mt-1 text-xs text-red-600 dark:text-red-400"> 
              {errors.type.message} 
            </p> 
          )} 
        </div> 
 
        {/* ── Start / End time ── */} 
        <div className="grid grid-cols-2 gap-3"> 
          <div> 
            <label 
              htmlFor="startTime" 
              className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300" 
            > 
              Start time 
            </label> 
            <input 
              id="startTime" 
              type="datetime-local" 
              {...register("startTime")} 
              aria-invalid={!!errors.startTime} 
              className={cn( 
                inputBase, 
                errors.startTime 
                  ? "border-red-500 dark:border-red-500" 
                  : "border-gray-300 dark:border-gray-600" 
              )} 
            /> 
            {errors.startTime && ( 
              <p className="mt-1 text-xs text-red-600 dark:text-red-400"> 
                {errors.startTime.message} 
              </p> 
            )} 
          </div> 
          <div> 
            <label 
              htmlFor="endTime" 
              className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300" 
            > 
              End time 
            </label> 
            <input 
              id="endTime" 
              type="datetime-local" 
              {...register("endTime")} 
              aria-invalid={!!errors.endTime} 
              className={cn( 
                inputBase, 
                errors.endTime 
                  ? "border-red-500 dark:border-red-500" 
                  : "border-gray-300 dark:border-gray-600" 
              )} 
            /> 
            {errors.endTime && ( 
              <p className="mt-1 text-xs text-red-600 dark:text-red-400"> 
                {errors.endTime.message} 
              </p> 
            )} 
          </div> 
        </div> 
 
        {/* ── Organiser Email ── */} 
        <div> 
          <label 
            htmlFor="organizerEmail" 
            className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300" 
          > 
            Organiser email 
          </label> 
          <input 
            id="organizerEmail" 
            type="email" 
            {...register("organizerEmail")} 
            aria-invalid={!!errors.organizerEmail} 
            className={cn( 
              inputBase, 
              errors.organizerEmail 
                ? "border-red-500 dark:border-red-500" 
                : "border-gray-300 dark:border-gray-600" 
            )} 
            placeholder="you@company.com" 
          /> 
          {errors.organizerEmail && ( 
            <p className="mt-1 text-xs text-red-600 dark:text-red-400"> 
              {errors.organizerEmail.message} 
            </p> 
          )} 
        </div> 
 
        {/* ── Description (optional) ── */} 
        <div> 
          <label 
            htmlFor="description" 
            className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300" 
          > 
            Description{" "} 
            <span className="font-normal text-gray-400 dark:text-gray-500"> 
              (optional) 
            </span> 
          </label> 
          <textarea 
            id="description" 
            rows={3} 
            {...register("description")} 
            aria-invalid={!!errors.description} 
            className={cn( 
              inputBase, 
              "resize-none", 
              errors.description 
                ? "border-red-500 dark:border-red-500" 
                : "border-gray-300 dark:border-gray-600" 
            )} 
            placeholder="Agenda, special requirements…" 
          /> 
          {errors.description && ( 
            <p className="mt-1 text-xs text-red-600 dark:text-red-400"> 
              {errors.description.message} 
            </p> 
          )} 
        </div> 
 
        {/* ── Submit ── */} 
        <button 
          type="submit" 
          disabled={isBusy} 
          className={cn( 
            "w-full rounded-md px-4 py-2 text-sm font-medium text-white transition-colors", 
            isBusy 
              ? "cursor-not-allowed bg-blue-400 dark:bg-blue-700" 
              : "bg-blue-600 hover:bg-blue-700 dark:bg-blue-600 dark:hover:bg-blue-500" 
          )} 
        > 
          {isBusy ? "Booking…" : "Book Room"} 
        </button> 
      </form> 
    </div> 
  ); 
}
