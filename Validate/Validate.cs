using System;

namespace validate
{
    public class Validate
    {
        public int ID { get; set; }

        public string StartDate { get; set; }

        public string EndDate { get; set; }

        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string Summary { get; set; }
    }
}
