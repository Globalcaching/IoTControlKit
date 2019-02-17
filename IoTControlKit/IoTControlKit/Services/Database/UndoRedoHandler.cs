using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Services.Database
{
    public class UndoRedoHandler
    {
        private int _maxTransactions = 10;
        private int _currentPos = 0;
        private List<TransactionInfo> _transactions;
        private TransactionInfo _currentTransaction;

        public UndoRedoHandler()
        {
            _transactions = new List<TransactionInfo>();
        }

        public bool CanUndo => _currentPos > 0;
        public bool CanRedo => _currentPos < _transactions.Count;

        public void StartTransaction()
        {
            _currentTransaction = new TransactionInfo();
        }

        public void EndTransaction()
        {
            if (_currentTransaction?.ContainsActions == true)
            {
                while (_transactions.Count > _currentPos)
                {
                    _transactions.RemoveAt(_transactions.Count() - 1);
                }

                _transactions.Add(_currentTransaction);
                while (_transactions.Count > _maxTransactions)
                {
                    _transactions.RemoveAt(0);
                }
                _currentPos = _transactions.Count;
            }
            _currentTransaction = null;
        }

        public TransactionInfo LastTransactionInfo()
        {
            if (_currentPos > 0)
            {
                return _transactions[_currentPos - 1];
            }
            return null;
        }

        public void Undo(NPoco.Database db)
        {
            if (_currentPos > 0)
            {
                _transactions[_currentPos-1].Undo(db);
                _currentPos--;
            }
        }

        public void Redo(NPoco.Database db)
        {
            if (_currentPos < _transactions.Count)
            {
                _transactions[_currentPos].Redo(db);
                _currentPos++;
            }
        }

        public void AddPoco(object poco)
        {
            var copiedItemString = Newtonsoft.Json.JsonConvert.SerializeObject(poco);
            var copiedItem = Newtonsoft.Json.JsonConvert.DeserializeObject(copiedItemString, poco.GetType());
            _currentTransaction?.AddPoco(copiedItem);
        }

        public void UpdatePoco(object orgPoco, object newPoco)
        {
            var copiedItemString = Newtonsoft.Json.JsonConvert.SerializeObject(newPoco);
            var copiedItem = Newtonsoft.Json.JsonConvert.DeserializeObject(copiedItemString, newPoco.GetType());
            _currentTransaction?.UpdatePoco(orgPoco, copiedItem);
        }

        public void DeletePoco(object poco)
        {
            var copiedItemString = Newtonsoft.Json.JsonConvert.SerializeObject(poco);
            var copiedItem = Newtonsoft.Json.JsonConvert.DeserializeObject(copiedItemString, poco.GetType());
            _currentTransaction?.DeletePoco(copiedItem);
        }
    }
}
