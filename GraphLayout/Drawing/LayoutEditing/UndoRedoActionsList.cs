using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Msagl.Drawing {
    class UndoRedoActionsList {
         UndoRedoAction currentUndo;

        internal UndoRedoAction CurrentUndo {
            get { return currentUndo; }
            set { currentUndo = value; }
        }
         UndoRedoAction currentRedo;

        internal UndoRedoAction CurrentRedo {
            get { return currentRedo; }
            set { currentRedo = value; }
        }

        internal UndoRedoAction AddAction(UndoRedoAction action) {
            if (CurrentUndo != null)
                CurrentUndo.Next = action;

            action.Previous = CurrentUndo;
            CurrentUndo = action;
            CurrentRedo = null;

            return action;
        }
    }
}
