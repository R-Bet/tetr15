namespace tetr15
{
    internal partial class Program
    {
        public class ScoreWithName : IComparable
        {
            public double Score { get; set; }
            public string Name { get; set; }

            public ScoreWithName(double Score, string Name)
            {
                this.Score = Score;
                this.Name = Name;
            }

            public static implicit operator ScoreWithName((double, string) ScoreWithName)
            {
                return new ScoreWithName(ScoreWithName.Item1, ScoreWithName.Item2);
            }

            public int CompareTo(object? obj)
            {
                ScoreWithName otherscore = obj as ScoreWithName;
                return this.Score > otherscore.Score ? -1 : 1;
            }
        }
    }
}
