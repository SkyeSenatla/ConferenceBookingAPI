# TechTalks — 90-Minute In-Class Revision Challenge

> **New project. No copy-pasting from ConferenceHub or CareerHub.**
> Build it from memory. Use the docs if you're stuck on syntax, not on understanding.

---

## What You Are Building

**TechTalks** is a mini developer-conference platform. Users can browse upcoming tech talks, see talk details, and register to attend. It is a different domain but uses every pattern you have practised over the last six days.

---

## Concept Map

| Part | Time | What You Demonstrate | Day(s) |
|------|------|----------------------|--------|
| 0 — Setup | 10 min | Project scaffolding | — |
| 1 — Components | 20 min | TypeScript interfaces, props, Tailwind, shadcn/ui, `useState` filter | W1 D1 + D2 |
| 2 — Data Fetching | 15 min | `QueryClientProvider`, `useQuery`, loading skeletons, error states | W1 D3 |
| 3 — Registration Form | 20 min | React Hook Form, Zod, `useMutation`, cache invalidation | W1 D4 |
| 4 — App Router | 15 min | Server Components, dynamic routes, `not-found.tsx`, `<Link>` | W2 D1 |
| Bonus | 10 min | `loading.tsx`, Suspense, Server Actions, `useActionState` | W2 D2 + D3 |

---

## Part 0 — Project Setup `(10 min)`

### 1. Bootstrap

```bash
npx create-next-app@latest techtalkss \
  --typescript --tailwind --app --src-dir \
  --import-alias "@/*" --no-git
cd techtalkss
```

### 2. Install dependencies

```bash
npm install @tanstack/react-query @tanstack/react-query-devtools
npm install react-hook-form @hookform/resolvers zod
npx shadcn@latest init -d
npx shadcn@latest add badge card button input label skeleton
```

### 3. Create `src/types/index.ts` — paste this in

```ts
export type TalkTopic = 'Frontend' | 'Backend' | 'DevOps' | 'AI/ML' | 'Mobile'

export interface Talk {
  id: number
  title: string
  speaker: string
  topic: TalkTopic
  duration: number        // minutes
  capacity: number
  registrationCount: number
  scheduledAt: string     // ISO 8601
  location: string
  description: string
}

export interface Registration {
  id: number
  talkId: number
  attendeeName: string
  attendeeEmail: string
  registeredAt: string
}
```

### 4. Create `src/lib/mock-data.ts` — paste this in

```ts
import { Talk, Registration } from '@/types'

const talks: Talk[] = [
  {
    id: 1,
    title: 'React 19 Deep Dive: The Compiler & Auto-Memoisation',
    speaker: 'Alice Nkosi',
    topic: 'Frontend',
    duration: 45,
    capacity: 60,
    registrationCount: 47,
    scheduledAt: '2026-08-12T09:00:00',
    location: 'Main Stage',
    description:
      'A hands-on look at what the React 19 compiler actually does, why it removes the need for useMemo/useCallback in most cases, and how to verify it is working.',
  },
  {
    id: 2,
    title: 'TypeScript Strict Mode Survival Guide',
    speaker: 'Brendan Osei',
    topic: 'Frontend',
    duration: 30,
    capacity: 40,
    registrationCount: 38,
    scheduledAt: '2026-08-12T11:00:00',
    location: 'Room A',
    description:
      'Real patterns for taming noImplicitAny, strictNullChecks, and discriminated unions in a production codebase.',
  },
  {
    id: 3,
    title: 'Next.js App Router: Server vs Client — Drawing the Line',
    speaker: 'Chloé Dupont',
    topic: 'Frontend',
    duration: 45,
    capacity: 60,
    registrationCount: 52,
    scheduledAt: '2026-08-12T14:00:00',
    location: 'Main Stage',
    description:
      'When to reach for "use client", why Server Components cannot read cookies or call hooks, and how Suspense boundaries let you stream partial UI.',
  },
  {
    id: 4,
    title: 'Zod v4: One Schema, Zero Duplication',
    speaker: 'Daniel Ferreira',
    topic: 'Backend',
    duration: 30,
    capacity: 35,
    registrationCount: 20,
    scheduledAt: '2026-08-13T10:00:00',
    location: 'Room B',
    description:
      'Why z.infer replaces hand-written TypeScript types, how to use .refine for cross-field validation, and a tour of what changed in v4.',
  },
  {
    id: 5,
    title: 'Deploying .NET APIs to Azure Container Apps',
    speaker: 'Fatima Al-Rashid',
    topic: 'DevOps',
    duration: 60,
    capacity: 30,
    registrationCount: 28,
    scheduledAt: '2026-08-13T13:00:00',
    location: 'Room C',
    description:
      'From dockerfile to health checks: packaging a .NET 10 minimal API and shipping it to Azure Container Apps with a managed identity.',
  },
  {
    id: 6,
    title: 'LLM-Powered Search with .NET + Semantic Kernel',
    speaker: 'Grace Mensah',
    topic: 'AI/ML',
    duration: 45,
    capacity: 50,
    registrationCount: 15,
    scheduledAt: '2026-08-13T15:30:00',
    location: 'Main Stage',
    description:
      'Building a hybrid semantic + keyword search pipeline using Semantic Kernel, pgvector, and a .NET 10 background service.',
  },
]

// ─── Simulated async API ──────────────────────────────────────────────────────

export async function fetchTalks(): Promise<Talk[]> {
  await delay(700)
  return talks
}

export async function fetchTalkById(id: number): Promise<Talk | null> {
  await delay(400)
  return talks.find((t) => t.id === id) ?? null
}

const registrations: Registration[] = []
let nextId = 1

export async function createRegistration(
  data: Omit<Registration, 'id' | 'registeredAt'>
): Promise<Registration> {
  await delay(900)
  // Simulate a duplicate-email error (optional — remove if it causes confusion)
  const duplicate = registrations.some(
    (r) => r.talkId === data.talkId && r.attendeeEmail === data.attendeeEmail
  )
  if (duplicate) throw new Error('You are already registered for this talk.')
  const reg: Registration = {
    ...data,
    id: nextId++,
    registeredAt: new Date().toISOString(),
  }
  registrations.push(reg)
  return reg
}

function delay(ms: number) {
  return new Promise((resolve) => setTimeout(resolve, ms))
}
```

---

## Part 1 — Components & Styling `(20 min)`

Work in **`src/app/page.tsx`** for now. You will move things to proper routes in Part 4.

### Task 1.1 — `TopicBadge` component

Create `src/components/TopicBadge.tsx`.

- Accepts a `topic: TalkTopic` prop.
- Renders a shadcn `<Badge>` with a different Tailwind colour class per topic:

| Topic | Colour hint |
|-------|-------------|
| Frontend | blue |
| Backend | green |
| DevOps | orange |
| AI/ML | purple |
| Mobile | pink |

**Acceptance criteria:** A `<TopicBadge topic="Frontend" />` renders a blue badge. A `<TopicBadge topic="DevOps" />` renders an orange badge.

---

### Task 1.2 — `TalkCard` component

Create `src/components/TalkCard.tsx`.

The card must display:
- Title (prominent)
- Speaker name
- `<TopicBadge>` for the topic
- Duration (e.g. `45 min`)
- Location
- Date and time (format the `scheduledAt` ISO string into something readable)
- Capacity bar or text: `47 / 60 registered`

**Props interface — you define it.** Think about what the card needs.

Style it with Tailwind. Use `className="..."` — no inline styles.

**Acceptance criteria:** Given any `Talk` object, the card renders all fields. The card looks reasonable at both mobile and desktop widths.

---

### Task 1.3 — Hardcoded list + topic filter in `page.tsx`

In `src/app/page.tsx`:

1. Add `"use client"` at the top.
2. Import the `talks` array directly from `@/lib/mock-data` (the named export, not the async function — for now).

   > **Hint:** You will need to export the raw array too. Add `export { talks }` at the bottom of `mock-data.ts`, **or** import the async result later when you add `useQuery`.

3. Render a responsive grid of `<TalkCard>` components using `.map()`. Give each a stable `key`.
4. Add a `useState` filter so the user can click a topic button to show only talks from that topic. An `"All"` option resets the filter.

**Acceptance criteria:** Six cards render in a grid. Clicking "Backend" hides the Frontend and DevOps cards.

---

## Part 2 — TanStack Query `(15 min)`

### Task 2.1 — `QueryClientProvider`

Create `src/app/providers.tsx`:

```tsx
// hint: "use client" goes here
// hint: QueryClient must be created inside useState so it is not shared across requests
```

Wire it into `src/app/layout.tsx` so every page is wrapped.

**Acceptance criteria:** React Query Devtools panel is visible in the bottom-right corner of the browser.

---

### Task 2.2 — Replace hardcoded data with `useQuery`

In `page.tsx`:

1. Remove the direct import of `talks`.
2. Call `useQuery` with:
   - `queryKey: ['talks']`
   - `queryFn: fetchTalks`
3. While `isPending` is true, render **three** `<Skeleton>` placeholders (use shadcn `Skeleton`, sized to match a card).
4. If `isError` is true, render a red error banner with `error.message`.
5. Once data arrives, render the filtered grid as before.

**Acceptance criteria:** On first load, three skeletons appear for ~700 ms, then the cards render. If you temporarily throw an error inside `fetchTalks`, the error banner appears.

---

## Part 3 — Registration Form `(20 min)`

### Task 3.1 — Zod schema

At the top of `src/components/RegisterForm.tsx`, define a Zod schema with:

| Field | Validation |
|-------|-----------|
| `attendeeName` | required string, min 2 chars |
| `attendeeEmail` | required, valid email |
| `talkId` | required number |

Infer a TypeScript type from the schema (`z.infer`). Do **not** write a separate interface.

---

### Task 3.2 — `RegisterForm` component

Build `src/components/RegisterForm.tsx`.

- Uses `useForm` with `zodResolver`.
- Accepts a `talkId: number` prop (pre-fills the hidden or select field).
- On submit calls `useMutation`:
  - `mutationFn` calls `createRegistration` from `@/lib/mock-data`.
  - `onSuccess` invalidates `['talks']` so the registration count updates.
- Shows field-level error messages (below each input).
- Shows a form-level red banner if `mutation.isError` is true.
- Shows a green success banner if `mutation.isSuccess` is true.
- The submit button is **disabled** while `isSubmitting || mutation.isPending` is true and shows `"Registering…"` text.

---

### Task 3.3 — Render the form in `page.tsx`

For now, add `<RegisterForm talkId={1} />` below the grid so you can test it.

**Acceptance criteria:**
- Submit with empty fields → field-level errors appear without a network call.
- Submit with a valid name and email → button shows "Registering…" for ~900 ms, then a green success message.
- Submit the same email again for the same talk → red error banner: "You are already registered for this talk."
- Open React Query Devtools and confirm the `['talks']` query is refetched after success.

---

## Part 4 — App Router `(15 min)`

### Task 4.1 — `/talks` list page (Server Component)

Create `src/app/talks/page.tsx`.

- **No `"use client"`** — this is a Server Component.
- `await fetchTalks()` directly in the component body.
- Render the grid of `<TalkCard>` components.
- Each card (or a button/link on the card) links to `/talks/[id]` using `<Link>`.

> **Checkpoint:** Can you still use `useState` in this file? Why not? Where does the filter need to live?

**Acceptance criteria:** Navigating to `/talks` shows all six cards without any loading state (data fetched server-side).

---

### Task 4.2 — `/talks/[id]` detail page

Create `src/app/talks/[id]/page.tsx`.

```tsx
// Page receives: { params: Promise<{ id: string }> }
// Remember to await params before using them (Next.js 15+)
```

- Fetch the talk with `fetchTalkById(Number(id))`.
- If `null` is returned, call `notFound()` (import from `next/navigation`).
- Render the full talk details (all fields).
- Render `<RegisterForm talkId={talk.id} />` below the details. 

  > **Hint:** `RegisterForm` uses `useMutation` and `useQueryClient` — it must be a Client Component. Does that force the whole page to be a Client Component? No. Import it as a regular component; Next.js will treat it as a boundary.

Create `src/app/talks/[id]/not-found.tsx`:
- A simple message: `"Talk not found."` with a link back to `/talks`.

**Acceptance criteria:** `/talks/3` shows Chloé's talk and the registration form. `/talks/999` shows the not-found page.

---

### Task 4.3 — Layout & navigation

In `src/app/layout.tsx`, add a `<nav>` with `<Link>` components:
- Home (`/`)
- Talks (`/talks`)

Style it with Tailwind so it is visible at the top of every page.

**Acceptance criteria:** The nav is visible on all routes. Clicking "Talks" goes to `/talks`; clicking a card goes to `/talks/[id]`.

---

## Bonus — Advanced Patterns `(10 min)`

Pick **any two** from this list:

### B1 — `loading.tsx` skeleton

Create `src/app/talks/loading.tsx`. Render a grid of six `<Skeleton>` cards. This file is shown automatically by Next.js while the Server Component on `/talks` is streaming. Test it by adding `await delay(2000)` temporarily inside `fetchTalks`.

---

### B2 — Suspense boundary for a "Similar Talks" section

In `/talks/[id]/page.tsx`, add a "Similar Talks" section that shows talks with the same topic.

- Move the similar-talks fetch into its own async component.
- Wrap it in `<Suspense fallback={<Skeleton ... />}>` so the main talk details render immediately.

---

### B3 — Server Action registration

Create `src/app/actions/registerForTalk.ts` with `"use server"` at the top.

The action should:
- Accept `(prevState: ActionState, formData: FormData)` where `ActionState = { status: 'success' | 'error'; message: string } | null`.
- Extract and validate the fields server-side (without Zod this time — manual checks only).
- Call `createRegistration`.
- Return `{ status: 'success', message: 'Registered!' }` or `{ status: 'error', message: '...' }`.

Create a `QuickRegisterForm` Client Component that uses `useActionState(registerForTalk, null)` instead of React Hook Form. Render it on the detail page alongside (or instead of) `RegisterForm`.

---

### B4 — Capacity full state

If `registrationCount >= capacity`, the `RegisterForm` should render a grey "Fully Booked" badge instead of the form.

---

## Grading Rubric

| Area | Marks | Criteria |
|------|-------|---------|
| TypeScript correctness | 2 | No `any`, all props typed, Zod inference used |
| Components (TalkCard, TopicBadge) | 2 | Renders all data, responsive, topic colours correct |
| TanStack Query | 2 | QueryClient wired, useQuery used, loading + error states handled |
| Form validation (Zod + RHF) | 2 | Field errors, form-level API error, disabled submit during flight |
| App Router | 2 | Server Components for list/detail, dynamic route, not-found page, navigation |
| **Bonus** | +2 | Any two bonus tasks working correctly |
| **Total** | **10 (+2)** | |

---

## Quick Reference

**Shadcn Skeleton:**
```tsx
import { Skeleton } from '@/components/ui/skeleton'
<Skeleton className="h-40 w-full rounded-xl" />
```

**shadcn Badge with custom colour:**
```tsx
import { Badge } from '@/components/ui/badge'
<Badge className="bg-blue-100 text-blue-800">Frontend</Badge>
```

**useQuery shape:**
```tsx
const { data, isPending, isError, error } = useQuery({
  queryKey: ['talks'],
  queryFn: fetchTalks,
})
```

**useMutation shape:**
```tsx
const mutation = useMutation({
  mutationFn: createRegistration,
  onSuccess: () => queryClient.invalidateQueries({ queryKey: ['talks'] }),
})
// call with: mutation.mutate(payload) or await mutation.mutateAsync(payload)
```

**Server Component data fetch:**
```tsx
// No hooks. No "use client". Just await.
export default async function Page() {
  const talks = await fetchTalks()
  return <div>{talks.map(...)}</div>
}
```

**Dynamic route params (Next.js 15+):**
```tsx
export default async function Page({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params
  // ...
}
```

**notFound():**
```tsx
import { notFound } from 'next/navigation'
if (!talk) notFound()
```
