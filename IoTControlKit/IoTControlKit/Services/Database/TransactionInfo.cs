using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Services.Database
{
    public class TransactionInfo
    {
        public List<ActionInfo> _actions;

        public TransactionInfo()
        {
            _actions = new List<ActionInfo>();
        }

        public bool ContainsActions => _actions.Any();

        public void AddPoco(object poco)
        {
            _actions.Add(new ActionInfo(ActionInfo.ActionType.Add, poco));
        }

        public void UpdatePoco(object orgPoco, object newPoco)
        {
            _actions.Add(new ActionInfo(orgPoco, newPoco));
        }

        public void DeletePoco(object poco)
        {
            _actions.Add(new ActionInfo(ActionInfo.ActionType.Delete, poco));
        }

        public void Undo(NPoco.Database db)
        {
            for (var index= _actions.Count-1; index>=0; index--)
            {
                _actions[index].Undo(db);
            }
        }

        public void Redo(NPoco.Database db)
        {
            foreach (var action in _actions)
            {
                action.Redo(db);
            }
        }

    }
}
