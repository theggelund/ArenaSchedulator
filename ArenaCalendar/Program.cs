using System;
using System.Collections.Generic;
using System.Linq;

namespace ArenaCalendar
{
    class Program
    {
        private static Func<Size, int, int, int> DefaultChildTeam = (size, day, hour) =>
        {
            if (day == 6 || day == 5)
            {
                return -100;
            }

            if (hour < 17)
            {
                return -100;
            }

            if (hour > 19)
            {
                return -100;
            }
            
            return 10;
        };
        
        private static Func<Size, int, int, int> DefaultOlderTeam = (size, day, hour) =>
        {
            if (day == 6 || day == 5)
            {
                return -100;
            }

            if (hour > 21)
            {
                return -100;
            }
            
            return 8;
        };
        
        private static Func<Size, int, int, int> OldBoysCalculation = (size, day, hour) =>
        {
            if (day != 7)
            {
                return -100;
            }

            if (hour != 21)
            {
                return -100;
            }
            
            return 8;
        };
        
        public static List<Entry> Entries = new List<Entry>
        {
            new Entry { Name = "G2012", Weight = DefaultChildTeam , Workouts = new List<Size>{ Size.Half } },
            new Entry { Name = "J2012", Weight = DefaultChildTeam, Workouts = new List<Size>{ Size.Half }  },
            new Entry { Name = "G2011", Weight = DefaultChildTeam, Workouts = new List<Size>{ Size.Half }},
            new Entry { Name = "J2011", Weight = DefaultChildTeam, Workouts = new List<Size>{ Size.Half }  },
            new Entry { Name = "G2010", Weight = DefaultChildTeam, Workouts = new List<Size>{ Size.Full, Size.Half}},
            new Entry { Name = "J2010", Weight = DefaultChildTeam, Workouts = new List<Size>{ Size.Half }  },
            new Entry { Name = "G2009", Weight = DefaultOlderTeam, Workouts = new List<Size>{ Size.Full, Size.Half}  },
            new Entry { Name = "J2009", Weight = DefaultOlderTeam, Workouts = new List<Size>{ Size.Full, Size.Half}  },
            new Entry { Name = "G2008", Weight = DefaultOlderTeam, Workouts = new List<Size>{ Size.Full, Size.Half}  },
            new Entry { Name = "G2007", Weight = DefaultOlderTeam, Workouts = new List<Size>{ Size.Full, Size.Half}  },
            new Entry { Name = "J07/J08", Weight = DefaultOlderTeam, Workouts = new List<Size>{ Size.Full, Size.Half}  },
            
            new Entry { Name = "J2006", Weight = DefaultOlderTeam, Workouts = new List<Size>{ Size.Full, Size.Half} },
            new Entry { Name = "J04/J05", Weight = DefaultOlderTeam, Workouts = new List<Size>{ Size.Full, Size.Half} },
            new Entry { Name = "J02/J03", Weight = DefaultOlderTeam, Workouts = new List<Size>{ Size.Full, Size.Half} },
            new Entry { Name = "Herrelaget", Weight = DefaultOlderTeam, Workouts = new List<Size>{ Size.Full, Size.Half} },
            new Entry { Name = "Damelaget", Weight = DefaultOlderTeam, Workouts = new List<Size>{ Size.Full, Size.Half} },
            new Entry { Name = "G2006", Weight = DefaultOlderTeam, Workouts = new List<Size>{ Size.Full, Size.Half} },
            new Entry { Name = "G2005", Weight = DefaultOlderTeam, Workouts = new List<Size>{ Size.Full, Size.Half} },
            new Entry { Name = "G2004", Weight = DefaultOlderTeam, Workouts = new List<Size>{ Size.Full, Size.Half} },
            new Entry { Name = "G2003", Weight = DefaultOlderTeam, Workouts = new List<Size>{ Size.Full, Size.Half} },
            new Entry { Name = "Junior Gutter", Weight = DefaultOlderTeam, Workouts = new List<Size>{ Size.Full, Size.Half} },
            new Entry { Name = "Old boys", Weight = OldBoysCalculation, Workouts = new List<Size>{ Size.Full, Size.Half} },

        };
        
        static void Main(string[] args)
        {
            Console.WriteLine(DateTime.Now.ToString("O"));
            
            Calculate(Entries);

            Console.WriteLine(DateTime.Now.ToString("O"));
            
        }

        private static void Calculate(List<Entry> entries)
        {
            var list = new List<TimeEntry>();
            
            // create week entries
            foreach (var day in new[] {1, 2, 3, 4, 5, 6, 7})
            {
                foreach (var hour in new[] {16, 17, 18, 19, 20, 21})
                {
                    list.Add(new TimeEntry {Day = day, Hour = hour});
                }
            }

            var usages = new Queue<Usage>();

            foreach (var entry in entries)
            {
                foreach (var size in entry.Workouts)
                {
                    usages.Enqueue(new Usage {Entry = entry, Size = size});
                }
            }

            while (usages.Count > 0)
            {
                var result = FindEntryLocation(usages.Dequeue(), list);

                foreach (var usage in result)
                {
                    usages.Enqueue(usage);
                }
            }

            WriteHours(list);
        }

        private static void WriteHours(List<TimeEntry> list)
        {
            foreach (var day in list.GroupBy(v => v.Day))
            {
                Console.WriteLine(day.Key);
                foreach (var hour in day.OrderBy(v => v.Hour))
                {
                    Console.WriteLine($"- {hour.Hour}");

                    foreach (var usage in hour.Usages)
                    {
                        Console.WriteLine($"  {usage.Usage.Entry.Name} {usage.Usage.Size}");
                    }
                }
            }
        }

        private static IEnumerable<Usage> FindEntryLocation(Usage usage, List<TimeEntry> list)
        {
            foreach (var day in new[] {1, 2, 3, 4, 5, 6, 7})
            {
                if (!IsNotCloserThanDays(list, day, 2, usage.Entry.Name))
                {
                    continue;
                }

                foreach (var hour in new[] {16, 17, 18, 19, 20, 21})
                {
                    var weight = CalculateWeight(usage.Entry, usage.Size, day, hour);

                    if (weight < 0)
                    {
                        continue;
                    }
                    
                    var timeEntry = list.First(v => v.Day == day && v.Hour == hour);

                    if (timeEntry.IsEmpty)
                    {
                        timeEntry.Usages.Add(new CourseUsage(usage, weight));

                        yield break;
                    }

                    int maxWeight = timeEntry.MaxWeight;
                    
                    // weight is larger, then change everything if full or change the lesser one if half
                    if (maxWeight < weight)
                    {
                        foreach (var replaced in ReplaceOrAddWhenWeightIsGreater(timeEntry, usage, weight))
                        {
                            yield return replaced;
                        }
                    }
                    else if (maxWeight == weight && CanFitWhenWeightIsEqual(timeEntry, usage))
                    {
                        timeEntry.Usages.Add(new CourseUsage(usage, weight));
                        yield break;
                    }
                    else if (maxWeight > weight && timeEntry.MaxSize != Size.Full)
                    {
                        if (timeEntry.Usages.Count < 2)
                        {
                            timeEntry.Usages.Add(new CourseUsage(usage, weight));                            
                        }
                        else
                        {
                            for (int i = 0; i < timeEntry.Usages.Count; i++)
                            {
                                var currentUsage = timeEntry.Usages[i];
                                if (currentUsage.Weight < weight)
                                {
                                    yield return currentUsage.Usage;
                                    timeEntry.Usages[i] = new CourseUsage(usage, weight);
                                    yield break;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static bool IsNotCloserThanDays(List<TimeEntry> list, int today, int days, string name)
        { 
            var timeEntry = list.FirstOrDefault(v => v.Usages.Any(u => u.Usage.Entry.Name == name));
            if (timeEntry == null)
            {
                return true;
            }

            return today - timeEntry.Day >= days;
        }

        private static IEnumerable<Usage> ReplaceOrAddWhenWeightIsGreater(TimeEntry entry, Usage usage, int weight)
        {
            if (usage.Size == Size.Full || entry.MaxSize == Size.Full)
            {
                foreach (var replace in entry.Usages)
                {
                    yield return replace.Usage;
                }
                
                entry.Usages = new List<CourseUsage> { new CourseUsage(usage, weight) };
                yield break;
            }

            if (entry.Usages.Count < 2)
            {
                entry.Usages.Add(new CourseUsage(usage, weight));
            }
            else
            {
                yield return entry.Usages[0].Usage;
                entry.Usages[0] = new CourseUsage(usage, weight);
            }
        }

        private static bool CanFitWhenWeightIsEqual(TimeEntry entry, Usage usage)
        {
            if (entry.MaxSize == Size.Full)
            {
                return false;
            }
            
            if (usage.Size == Size.Half)
            {
                if (entry.Usages.Count <= 1)
                {
                    return true;
                }
            }
            else
            {
                if (entry.Usages.Count == 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CalculateWeight(Entry entry, Size size, int day, int hour)
        {
            return entry.Weight(size, day, hour);
        }
    }

    public class Usage
    {
        public virtual Entry Entry { get; set; }
        
        public virtual Size Size { get; set; }
    }

    public class CourseUsage 
    {
        public CourseUsage(Usage usage, int weight)
        {
            Usage = usage;
            Weight = weight;
        }

        private CourseUsage()
        {
        }
        
        public Usage Usage { get; set; }
        
        public int Weight { get; set; }
    }

    public class TimeEntry
    {
        public List<CourseUsage> Usages { get; set; } = new List<CourseUsage>();
        
        public int Hour { get; set; }
        
        public int Day { get; set; }

        public int MaxWeight => Usages.Count == 0 ? 0 : Usages.Max(v => v.Weight);

        public int MinWeight => Usages.Count == 0 ? 0 :Usages.Min(v => v.Weight);

        public Size MaxSize => Usages.Count == 0 ? Size.None : Usages.Where(v => v.Usage != null).Max(v => v.Usage.Size);

        public bool IsEmpty => Usages == null || Usages.Count == 0;
    }

    public class Entry
    {
        public string Name { get; set; }
        
        public Func<Size, int, int, int> Weight { get; set; }

        public List<Size> Workouts { get; set; }
    }

    public enum Size
    {
        None = 0,
        Half = 1,
        Full = 2
    }
}