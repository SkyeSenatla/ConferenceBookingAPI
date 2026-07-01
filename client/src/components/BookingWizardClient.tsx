"use client";

import dynamic from "next/dynamic";

const BookingWizard = dynamic(
  () => import("@/components/BookingWizard").then((mod) => ({ default: mod.BookingWizard })),
  {
    loading: () => (
      <div className="animate-pulse rounded-lg border border-gray-200 bg-white p-6 dark:border-gray-700 dark:bg-gray-800 h-96" />
    ),
    ssr: false,
  }
);

export { BookingWizard };
