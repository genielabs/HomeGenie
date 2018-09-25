using System;
using System.Collections.Generic;
using HomeGenie.Automation.Scheduler;
using NUnit.Framework;

namespace HomeGenieTests
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
            _start = new DateTime(2017, 01, 01);
        }

        [Test]
        public void BasicCronExpression()
        {
            var expression = "0 * * * *";
            var occurences = GetOccurencesForDate(_scheduler, _start, expression);

            DisplayOccurences(expression, occurences);
            Assert.That(occurences.Count, Is.EqualTo(24));
        }

        [Test]
        [TestCase("(30 22 * * *) > (49 22 * * *)", 20)]
        [TestCase("(50 23 * * *) > (9 0 * * *)", 20)]
        [TestCase("(50 23 * * *) > (9 1 * * *)", 80)]
        public void CronExpressionWithSpan(string expression, int expectedOccurences)
        {
            var occurences = GetOccurencesForDate(_scheduler, _start, expression);

            DisplayOccurences(expression, occurences);
            Assert.That(occurences.Count, Is.EqualTo(expectedOccurences));
        }

        [Test]
        [TestCase("(30 * * * *) ; (* 22,23 * * *)")]
        [TestCase("(30 * * * *) & (* 22,23 * * *)")]
        public void CronExpressionWithAnd(string expression)
        {
            var occurences = GetOccurencesForDate(_scheduler, _start, expression);

            DisplayOccurences(expression, occurences);
            Assert.That(occurences.Count, Is.EqualTo(2));
        }

        [Test]
        [TestCase("(30 22 * * *) : (49 22 * * *)")]
        [TestCase("(30 22 * * *) | (49 22 * * *)")]
        public void CronExpressionWithOr(string expression)
        {
            var occurences = GetOccurencesForDate(_scheduler, _start, expression);

            DisplayOccurences(expression, occurences);
            Assert.That(occurences.Count, Is.EqualTo(2));
        }

        [Test]
        [TestCase("(30 * * * *) % (* 1-12 * * *)")]
        [TestCase("(30 * * * *) ! (* 1-12 * * *)")]
        public void CronExpressionWithExcept(string expression)
        {
            var occurences = GetOccurencesForDate(_scheduler, _start, expression);

            DisplayOccurences(expression, occurences);
            Assert.That(occurences.Count, Is.EqualTo(12));
        }

        private static List<DateTime> GetOccurencesForDate(SchedulerService scheduler, DateTime date, string expression)
        {
            date = DateTime.SpecifyKind(date, DateTimeKind.Local);
            return scheduler.GetScheduling(date.Date, date.Date.AddHours(24).AddMinutes(-1), expression);
        }

        private void DisplayOccurences(string cronExpression, List<DateTime> occurences)
        {
            Console.WriteLine("Cron expression: {0}", cronExpression);
            foreach (var dateTime in occurences)
            {
                Console.WriteLine(dateTime.ToString("yyyy.MM.dd HH:mm:ss"));
            }
        }
    }
}