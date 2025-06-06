using System;
using System.ComponentModel.DataAnnotations;

namespace WeatherApp.Models
{
    public class WeatherData
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string City { get; set; } = string.Empty;

        public string? ZipCode { get; set; }

        [Required]
        public double TemperatureCelsius { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        public DateTime RetrievedAt { get; set; }
    }
}