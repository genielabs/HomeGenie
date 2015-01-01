using System;
using HomeGenie.Service;

namespace HomeGenie.Data
{
    public class ValueHistory
    {
        public class HistoryValue
        {
            string Value;
            DateTime TimeStamp;

            public HistoryValue(string value, DateTime timeStamp)
            {
                Value = value;
                TimeStamp = timeStamp;
            }
        }

        private TsList<HistoryValue> valueHistory = new TsList<HistoryValue>();
        private int historyLimit = 10;
        private HistoryValue lastEmpty;
        private HistoryValue lastNonEmpty;

        public ValueHistory()
        {
        }

        public int Limit
        {
            get { return historyLimit; }
            set { historyLimit = value; }
        }

        public TsList<HistoryValue> History
        {
            get { return valueHistory; }
        }

        public void AddValue(string value, DateTime timeStamp)
        {
            valueHistory.Insert(0, new HistoryValue(value, timeStamp));
            //
            if (value == "0" || string.IsNullOrWhiteSpace(value))
            {
                lastEmpty = new HistoryValue(value, timeStamp);
            }
            else
            {
                lastNonEmpty = new HistoryValue(value, timeStamp);
            }
            //
            while (valueHistory.Count > historyLimit)
            {
                valueHistory.RemoveAt(valueHistory.Count - 1);
            }
        }

        public HistoryValue LastValue
        {
            get { return (valueHistory.Count > 0 ? valueHistory[0] : null); }
        }

        public HistoryValue LastEmptyValue
        {
            get { return lastEmpty; }
        }

        public HistoryValue LastNonEmptyValue
        {
            get { return lastNonEmpty; }
        }
    }
}

