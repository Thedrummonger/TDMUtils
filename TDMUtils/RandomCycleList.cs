using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDMUtils
{
    public class RandomCycleList<T>
    {
        public event Action? ListUpdated;
        public RandomCycleList(IEnumerable<T> source, double RefreshPercent)
        {
            Source = source.ToList();
            refreshDec = RefreshPercent;
            ResetAll();
            rnd = new Random();
        }
        public RandomCycleList()
        {
            Source = new List<T>();
            refreshDec = 0.6;
            ResetAll();
            rnd = new Random();
        }
        public double refreshDec;
        public List<T> Source;
        public List<T> Unused = [];
        public List<T> Used = [];
        private Random rnd;
        [JsonIgnore]
        public int MaxUsed { get { return (int)(Source.Count * refreshDec); } }

        public void Override(RandomCycleList<T> Target)
        {
            refreshDec = Target.refreshDec;
            Source = Target.Source;
            Used = Target.Used;
            Unused = Target.Unused;
            ListUpdated?.Invoke();
        }

        public T? GetRandomUnused()
        {
            if (Source.Count < 1) { return default; }
            return GetUnused(rnd.Next(Unused.Count));
        }
        public T GetUnused(int Index)
        {
            T Candidate = Unused[Index];
            Used.Add(Candidate);
            Unused.RemoveAt(Index);
            RefreshOldest();
            ListUpdated?.Invoke();
            return Candidate;
        }

        public void AddNew(T input)
        {
            Source.Add(input);
            Unused.Add(input);
            RefreshOldest();
            ListUpdated?.Invoke();
        }

        public void Remove(T target)
        {
            Source.Remove(target);
            Unused.Remove(target);
            Used.Remove(target);
            RefreshOldest();
            ListUpdated?.Invoke();
        }

        public void ResetAll()
        {
            Unused.Clear();
            Used.Clear();
            Unused = new List<T>(Source);
            ListUpdated?.Invoke();
        }

        public T SetMessageUnused(int Index)
        {
            T Candidate = Used[Index];
            Unused.Add(Candidate);
            Used.RemoveAt(Index);
            RefreshOldest();
            ListUpdated?.Invoke();
            return Candidate;
        }
        public T[] SetMessagesUnused(IEnumerable<int> Indexes)
        {
            List<T> Candidates = new List<T>();
            foreach (int Index in Indexes)
            {
                T Candidate = Used[Index];
                Candidates.Add(Candidate);
            }
            foreach(T Candidate in Candidates)
            {
                Unused.Add(Candidate);
                Used.Remove(Candidate);
            }
            RefreshOldest();
            ListUpdated?.Invoke();
            return [.. Candidates];
        }

        public T SetMessageUsed(int Index)
        {
            T Candidate = Unused[Index];
            Used.Add(Candidate);
            Unused.RemoveAt(Index);
            RefreshOldest();
            ListUpdated?.Invoke();
            return Candidate;
        }

        public T[] SetMessagesUsed(IEnumerable<int> Indexes)
        {
            List<T> Candidates = new List<T>();
            foreach (var Index in Indexes)
            {
                T Candidate = Unused[Index];
                Candidates.Add(Candidate);
            }
            foreach (T Candidate in Candidates)
            {
                Used.Add(Candidate);
                Unused.Remove(Candidate);
            }
            RefreshOldest();
            ListUpdated?.Invoke();
            return [.. Candidates];
        }

        private void RefreshOldest()
        {
            if (Used.Count != 0 && Used.Count > MaxUsed)
            {
                T Oldest = Used[0];
                Used.RemoveAt(0);
                Unused.Add(Oldest);
            }
        }
    }
}
