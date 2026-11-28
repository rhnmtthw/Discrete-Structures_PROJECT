# vacanTEE_DS2: Clinic Appointment Scheduler

Backend for a Discrete Structures 2 (DS2) group project. Given the procedures a patient needs (for example blood lab, X-ray, consultation) and the time the patient arrives, the backend finds every conflict-free ordering of clinic time slots and picks the one with the least waiting time.

**Team**
- Patrick Nathaniel Dimandal: Engine 2 (recursion and induction)
- Rohan Matthew Mulingbayan: Engine 1 (counting and permutations)

Built with C# on ASP.NET Core MVC, targeting **.NET 10**.

## DS2 concepts used

| Concept | Where it is applied |
|---|---|
| Fundamental Counting Principle | Engine 1: counts raw combinations across departments |
| Permutations P(n, r) | Engine 1: counts and generates ordered slot arrangements |
| Recursive definition | Engine 2: cumulative wait time W_k |
| Mathematical induction | Engine 2: induction explanation of the wait recurrence |

All results are computed live from the input. Nothing is hardcoded.

## Project structure

```
Controllers/HomeController.cs      Test() action that runs all test cases
Data/ClinicMockData.cs             Mock clinic dataset: departments, durations, open slots, time helpers
Engines/CombinatoricsEngine.cs     Engine 1
Engines/QueueRecurrenceEngine.cs   Engine 2
Models/ClinicSlot.cs               One open clinic slot
Models/Engine1Result.cs            Engine 1 output (consumed by Engine 2)
Models/ScheduleStep.cs             One step of the optimal schedule (Engine 2 output)
```

All times are stored as integer **minutes from midnight** (480 = 08:00 AM), so overlap checks are plain integer arithmetic.

## Engine 1: Combinatorics (FCP and permutations)

`CombinatoricsEngine.Run(allSlots, requiredDepts, arrivalMins)` runs the whole pipeline:

1. **Input guard.** An empty request (r = 0) or r > n returns `Status = "ERROR"` with a clear message, with no exception thrown.
2. **Filter** to slots in the required departments that start at or after arrival, which gives n usable slots.
3. **FCP** (`CountFcp`): multiplies the number of slots in each required department.
4. **P(n, r)** (`CountPermutations`): n x (n-1) x ... x (n-r+1), which is r terms. No full factorial is ever computed, so there is no overflow and no 0! special case.
5. **Generate** all ordered arrangements by backtracking (`GeneratePermutations`).
6. **Feasibility filter** (`IsValid`) keeps a sequence only if:
   - the slot at position k belongs to the k-th required department,
   - the first slot starts at or after the patient's arrival,
   - each slot starts at or after the previous slot ends (no overlap).
7. Returns an `Engine1Result` with the explanation strings and the list of valid schedules.

Worked example for LAB, RAD, CON arriving at 08:00: the FCP gives 12 raw combinations, P(7, 3) gives 210 ordered arrangements, and 5 of them are valid schedules.

## Engine 2: Recursion and induction

`QueueRecurrenceEngine.Run(engine1Result, arrivalMins)` computes the cumulative wait W_k of every valid schedule recursively:

```
W_1 = max(0, Start_1 - T_arrival)
W_k = W_(k-1) + max(0, Start_k - (Start_(k-1) + S_(k-1)))
```

It returns the schedule with the smallest total wait, with a step-by-step trace and an induction explanation.

## Running it

1. Open the solution in Visual Studio (or run `dotnet run` in the project folder).
2. Start the app and browse to `/Home/Test`.

The page runs every test case and prints the results as plain text.

## Test cases

| Case | Input | Expected |
|---|---|---|
| TC-1 | LAB, RAD, CON, arrival 08:00 | 12 raw, P(7,3) = 210, 5 valid; Engine 2 picks LAB-01, RAD-01, CON-01 with total wait 10 |
| TC-2 | RAD, CON, arrival 08:50 | Early slots excluded; 2 raw, P(3,2) = 6, 1 valid |
| TC-5a | empty selection (r = 0) | `ERROR`: at least one procedure is required |
| TC-5b | LAB, RAD, CON, arrival 03:00 PM | `ERROR`: r cannot exceed n (n = 0) |
| Large | LAB, ECG, RAD, PUL, CON, arrival 08:00 | 24 raw, P(10,5) = 30,240 generated, 0 valid (no schedule fits that order) |
| Zero wait | ECG, CON, arrival 01:00 PM | Total wait 0 (CON-02 starts exactly when ECG-02 ends) |

## Known limitations

- Engine 1 generates every P(n, r) arrangement and then filters, which is brute force. It is fast at clinic scale (about 10 ms for P(10, 5)) but grows quickly for large n. A faster version would prune invalid branches inside the backtracking.
- The department list and durations are mock data in `ClinicMockData.cs`, not a real clinic database.
