using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IoTControlKit.Services.Database
{
    public class ActionInfo
    {
        public enum ActionType
        {
            Add,
            Delete,
            Update
        }

        private ActionType _actionType;
        private object _orgPoco = null;
        private object _newPoco = null;

        public ActionType CrudAction => _actionType;
        public object OrgPoco => _orgPoco;
        public object NewPoco => _newPoco;

        public ActionInfo(object orgPoco, object newPoco)
        {
            _actionType = ActionType.Update;
            _orgPoco = orgPoco;
            _newPoco = newPoco;
        }

        public ActionInfo(ActionType action, object poco)
        {
            _actionType = action;
            if (action == ActionType.Add)
            {
                _newPoco = poco;
            }
            else
            {
                _orgPoco = poco;
            }
        }

        public void Undo(NPoco.Database db)
        {
            switch (_actionType)
            {
                case ActionType.Add:
                    db.Delete(_newPoco);
                    break;
                case ActionType.Update:
                    db.Update(_newPoco);
                    break;
                case ActionType.Delete:
                    db.Update(_orgPoco);
                    break;
            }
        }

        public void Redo(NPoco.Database db)
        {
            switch (_actionType)
            {
                case ActionType.Add:
                    db.Insert(_newPoco);
                    break;
                case ActionType.Update:
                    db.Update(_newPoco);
                    break;
                case ActionType.Delete:
                    db.Delete(_orgPoco);
                    break;
            }
        }
    }
}
