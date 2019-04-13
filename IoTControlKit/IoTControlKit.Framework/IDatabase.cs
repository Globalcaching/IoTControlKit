using System;
using System.Collections.Generic;
using System.Text;

namespace IoTControlKit.Framework
{
    public interface IDatabase
    {
        bool ExecuteWithinTransaction(Action<NPoco.Database, Guid> action, object caller = null, Action<bool> executeAfterTransaction = null, bool isUndoRedoAction = false);
        void Execute(Action<NPoco.Database> action);
    }
}
