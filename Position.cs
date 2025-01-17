namespace tetr15
{
    internal partial class Program
    {
        public class Position
        {
            public int x, y;
            public Position(int x, int y)
            {
                this.x = x; this.y = y;
            }

            public Position Clone()
            {
                return new Position(x, y);
            }

            public static implicit operator Position(ValueTuple<int, int> tuple)
            {
                return new Position(tuple.Item1, tuple.Item2);
            }
        }
    }
}
