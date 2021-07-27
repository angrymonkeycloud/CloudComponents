using System.Collections.Generic;

namespace AngryMonkey.Cloud.Components
{
    public partial class DateTimePicker
    {
        protected class DecadeYears
        {
            public int Year { get; set; }
            public int StartDecadeYear { get; set; }
            public int EndDecadeYear { get; set; }
            public bool CurrentDeacde { get; set; }

            public bool CurrentYear { get; set; }
            public string Classes
            {
                get
                {
                    List<string> classes = new();
                    if (CurrentDeacde)
                        classes.Add("_currentdecade");
                    if (CurrentYear)
                        classes.Add("_currentyear");

                    return string.Join(' ', classes);
                }
            }

            public DecadeYears(int year, int startDecadeYear, int endDecadeYear)
            {
                Year = year;
                StartDecadeYear = startDecadeYear;
                EndDecadeYear = endDecadeYear;
            }
        }
    }
}
