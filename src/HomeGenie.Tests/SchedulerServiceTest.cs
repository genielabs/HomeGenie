using System;
using System.Collections.Generic;

using NUnit.Framework;

using HomeGenie.Automation.Scheduler;

namespace HomeGenie.Tests
{
    [TestFixture]
    public class SchedulerServiceTest
    {
        private SchedulerService _scheduler;
        private DateTime _start;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _scheduler = new SchedulerService(null);
            _start = new DateTime(2022, 01, 01);
        }

        [Test]
        public void BasicCronExpression()
        {
            var expression = "0 * * * *";
            var occurrences = GetOccurrencesForDate(_scheduler, _start, expression);

            DisplayOccurrences(expression, occurrences);
            Assert.That(occurrences.Count, Is.EqualTo(24));
        }

        [Test]
        [TestCase("(30 22 * * *) > (49 22 * * *)", 20)]
        [TestCase("(50 23 * * *) > (9 0 * * *)", 20)]
        [TestCase("(50 23 * * *) > (9 1 * * *)", 80)]
        [TestCase("(0 0 * * *) > (59 23 * * *)", 1440)]
        public void CronExpressionWithSpan(string expression, int expectedOccurrences)
        {
            var occurrences = GetOccurrencesForDate(_scheduler, _start, expression);

            DisplayOccurrences(expression, occurrences);
            Assert.That(occurrences.Count, Is.EqualTo(expectedOccurrences));
        }

        [Test]
        [TestCase("(30 * * * *) ; (* 22,23 * * *)")]
        [TestCase("(30 * * * *) & (* 22,23 * * *)")]
        public void CronExpressionWithAnd(string expression)
        {
            var occurrences = GetOccurrencesForDate(_scheduler, _start, expression);

            DisplayOccurrences(expression, occurrences);
            Assert.That(occurrences.Count, Is.EqualTo(2));
        }

        [Test]
        [TestCase("(30 22 * * *) : (49 22 * * *)")]
        [TestCase("(30 22 * * *) | (49 22 * * *)")]
        public void CronExpressionWithOr(string expression)
        {
            var occurrences = GetOccurrencesForDate(_scheduler, _start, expression);

            DisplayOccurrences(expression, occurrences);
            Assert.That(occurrences.Count, Is.EqualTo(2));
        }

        [Test]
        [TestCase("(30 * * * *) % (* 1-12 * * *)")]
        [TestCase("(30 * * * *) ! (* 1-12 * * *)")]
        public void CronExpressionWithExcept(string expression)
        {
            var occurrences = GetOccurrencesForDate(_scheduler, _start, expression);

            DisplayOccurrences(expression, occurrences);
            Assert.That(occurrences.Count, Is.EqualTo(12));
        }
        
        [Test]
        [TestCase("[ (* * 1-31 11 *) : (* * 1-15 1 *) : (* * * 12 *) ]")]
        public void CronExpressionAcrossYears(string expression)
        {
            var occurrences = _scheduler.GetScheduling(new DateTime(2018, 6, 1), new DateTime(2019, 6, 30), expression);
            //DisplayOccurrences(expression, occurrences);
            Assert.That(occurrences.Count, Is.EqualTo((30+31+15)*1440));
        }

        private static List<DateTime> GetOccurrencesForDate(SchedulerService scheduler, DateTime date, string expression)
        {
            date = DateTime.SpecifyKind(date, DateTimeKind.Local);
            Console.WriteLine("Date range {0} - {1}", date.Date, date.Date.AddHours(24).AddSeconds(-1));
            return scheduler.GetScheduling(date.Date, date.Date.AddHours(24).AddSeconds(-1), expression);
        }

        private void DisplayOccurrences(string cronExpression, List<DateTime> occurences)
        {
            Console.WriteLine("Cron expression: {0}", cronExpression);
            foreach (var dateTime in occurences)
            {
                Console.WriteLine(dateTime.ToString("yyyy.MM.dd HH:mm:ss"));
            }
        }
    }
}
