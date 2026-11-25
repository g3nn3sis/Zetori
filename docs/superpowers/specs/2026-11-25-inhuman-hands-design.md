# Inhuman hands signal

## Intent

Cheat menus have a "Prediction" slider that pushes a player's hands ahead along their swing, so they reach
and push further than their real hands. From outside it looks like exaggerated, inhuman hand movement. The
caster wants Zetori to flag it as a cheat signal, including hands that are just too fast or that accelerate
instantly.

## Goals, non-goals

- Goal: a new cheat signal, "Hand motion", with its own alert toggle ("Inhuman Hands"), report line and replay
  highlight, like the other signals.
- Goal: no false flags on fast legit players.
- Non-goal: body speed. The existing Speed signal covers that.
- Non-goal: catching a prediction cheater who barely moves. Prediction only shows when the hands move fast.

## Approaches

- A. Hand speed and acceleration limits only. These match the request directly, but network jitter makes
  single-update readings noisy.
- B. Straight-arm test only: a hand still flying outward when it's already far from the head. This is
  specific to prediction, but misses inward and sideways motion.
- **C. Both, as one signal (picked by the caster).**

## Design

- Input: each network update (`SerializeReadShared` postfix). The head and each hand's synced position are
  taken in rig space, rotated by the synced body rotation, so body movement and turning don't count. This
  is the same frame the replay recorder stores as the raw limb position.
- Each hand's velocity is measured over the newest sample at least 0.2 s older, but no more than 0.5 s older.
  Acceleration is the change between that velocity and one measured at least 0.2 s earlier.
- Three checks, all scale-normalised:
  - fast: hand speed ≥ 10 m/s
  - jerk: acceleration ≥ 85 m/s², counted each time it starts
  - fling: distance from the head ≥ 1.3 and outward speed ≥ 6 m/s
- Each check feeds a score that fades with a 30 s time constant. The signal is raised, and stays raised,
  when fast ≥ 1 s, jerks ≥ 5 or fling ≥ 1 s.

## Calibration

Measured from three replays with 22 legit players, between 13 s and 176 s each, by driving the compiled
`HandMotion` with recorded raw hand positions:

- Legit worst case: top windowed speed 9.3 m/s, 0 s fast, 0 jerks, 0.3 s fling. None were flagged.
- Simulated prediction (hand + k × velocity): k = 0.05 flags 4 of 24, and k = 0.1 flags 7 of 24. The ones
  flagged are the players who swing hard. Idle players stay unflagged even with prediction, as intended.

## Sensitivity raised 3×

At the caster's request, each flag limit was divided by 3: fast ≥ 1/3 s, jerks ≥ 5/3 (two close together)
and fling ≥ 1/3 s. Re-scored on the saved per-player peaks from the same replays:

- Legit players: none flagged. The closest was a fling peak of 0.25 s against the 0.33 s limit, so the margin
  is now thin.
- Simulated prediction: k = 0.03 flags 4 of 24 (was 0), k = 0.05 flags 5 (was 4), and k = 0.1 flags 12 (was 7).

## Arm reach change

Arm reach (the separate Long arms signal) now only counts a hand that touches a surface, using the same
contact check as hovering. A hand floating in the air is ignored, since a disconnected controller can drift
away from the body.

## Left open

- These limits are verified offline only. They need a real cheater's replay to confirm.
