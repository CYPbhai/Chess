# Chess 3D

A fully 3D chess game built in Unity (C#) featuring 
a hand-crafted AI engine and multiple game modes.

## AI Engine
- Minimax algorithm with Alpha-Beta Pruning (depth 3-5)
- MVV-LVA move ordering for optimized search
- Legal move validation with check/checkmate detection
- Board evaluation using piece-weight heuristics
- Center control bonus strategy
- Repetition penalty to avoid draw loops
- Async AI thinking — no UI freezing during search

## Game Modes
- Human vs AI
- Human vs Human
- AI vs AI

## Difficulty Levels
| Level  | Search Depth |
|--------|-------------|
| Easy   | 3           |
| Medium | 4           |
| Hard   | 5           |

## Built With
- Unity Engine
- C#
- .NET Tasks (async/await)


## Gameplay
[Watch Gameplay Video](https://drive.google.com/file/d/1yz8aZ8_KOpZXpOLyK5PPWVUpTO0E1sx8/view?usp=drive_link)
