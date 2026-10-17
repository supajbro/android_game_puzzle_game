using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JellyRiot.Core
{
    public enum PlacementFailure
    {
        None = 0,
        InvalidColumn = 1,
        ColumnFull = 2,
        InvalidColour = 3
    }

    public enum BoardEventType
    {
        JellyPlaced = 0
    }

    [Serializable]
    public readonly struct BoardEvent
    {
        public BoardEvent(BoardEventType type, BoardCoordinate coordinate, JellyColour colour)
        {
            Type = type;
            Coordinate = coordinate;
            Colour = colour;
        }

        public BoardEventType Type { get; }

        public BoardCoordinate Coordinate { get; }

        public JellyColour Colour { get; }
    }

    [Serializable]
    public sealed class MoveResult
    {
        private static readonly ReadOnlyCollection<BoardEvent> NoEvents =
            new ReadOnlyCollection<BoardEvent>(Array.Empty<BoardEvent>());

        private MoveResult(bool succeeded, PlacementFailure failure, int turnNumber, BoardCoordinate? placedAt,
            IReadOnlyList<BoardEvent> events)
        {
            Succeeded = succeeded;
            Failure = failure;
            TurnNumber = turnNumber;
            PlacedAt = placedAt;
            Events = events;
        }

        public bool Succeeded { get; }

        public PlacementFailure Failure { get; }

        public int TurnNumber { get; }

        public BoardCoordinate? PlacedAt { get; }

        public IReadOnlyList<BoardEvent> Events { get; }

        internal static MoveResult Rejected(PlacementFailure failure, int turnNumber) =>
            new MoveResult(false, failure, turnNumber, null, NoEvents);

        internal static MoveResult Placed(BoardCoordinate coordinate, JellyColour colour, int turnNumber)
        {
            var events = new ReadOnlyCollection<BoardEvent>(new[]
            {
                new BoardEvent(BoardEventType.JellyPlaced, coordinate, colour)
            });
            return new MoveResult(true, PlacementFailure.None, turnNumber, coordinate, events);
        }
    }
}
