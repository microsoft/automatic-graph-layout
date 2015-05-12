using System.ComponentModel;

//using TestWpfViewer.Annotations;

namespace Microsoft.Msagl.Layout.LargeGraphLayout
{
    public class ILayerInfo : INotifyPropertyChanged
    {
        public virtual string getLabel()
        {
            return "";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

         bool _isVisible;

        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                _isVisible = value;
                OnPropertyChanged("IsVisible");
            }
        }

         bool _isActive;

        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                _isActive = value;
                OnPropertyChanged("IsActive");
            }
        }
    }

    public class ZoomLayerInfo : ILayerInfo
    {
        public int ZoomLevel;

        //public List<ISublayerInfo> SublayerInfos = new List<ISublayerInfo>(); 
         NodesSublayerInfo _nodesSublayerInfo;

        public NodesSublayerInfo NodesSlInf
        {
            get { return _nodesSublayerInfo; }
            set { _nodesSublayerInfo = value; }
        }

         EdgesSublayerInfo _edgesSublayerInfo;

        public EdgesSublayerInfo EdgesSlInf
        {
            get { return _edgesSublayerInfo; }
            set { _edgesSublayerInfo = value; }
        }

         CellsSublayerInfo _cellsSublayerInfo;
        public CellsSublayerInfo CellsSlInf
        {
            get { return _cellsSublayerInfo; }
            set { _cellsSublayerInfo = value; }
        }

        public ZoomLayerInfo(int zoomLevel)
        {
            ZoomLevel = zoomLevel;
            _nodesSublayerInfo = new NodesSublayerInfo();
            _edgesSublayerInfo = new EdgesSublayerInfo();
            _cellsSublayerInfo = new CellsSublayerInfo();
        }

        override public string getLabel()
        {
            return "level " + ZoomLevel;
        }
    }

    public class SkeletonLayerInfo : ILayerInfo
    {
        public int ZoomLevel;
        override public string getLabel()
        {
            return "skeleton of level " + ZoomLevel;
        }

        public SkeletonLayerInfo(int zoomLevel)
        {
            ZoomLevel = zoomLevel;
        }
    }

    public class LooseLayerInfo : ILayerInfo
    {
        override public string getLabel()
        {
            return "unassigned";
        }
    }

    public interface ISublayerInfo
    {
        string getLabel();
    }

    public class NodesSublayerInfo : ISublayerInfo, INotifyPropertyChanged
    {
         bool _isVisible = true;

        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                _isVisible = value;
                OnPropertyChanged("IsVisible");
            }
        }
        public string getLabel()
        {
            return "nodes";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        //[NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName = null)//[CallerMemberName]
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class EdgesSublayerInfo : ISublayerInfo, INotifyPropertyChanged
    {
         bool _isVisible;

        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                _isVisible = value;
                OnPropertyChanged("IsVisible");
            }
        }
        public string getLabel()
        {
            return "edges";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        //[NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName = null)//[CallerMemberName]
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CellsSublayerInfo : ISublayerInfo, INotifyPropertyChanged
    {
         bool _isVisible = false;

        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                _isVisible = value;
                OnPropertyChanged("IsVisible");
            }
        }
        public string getLabel()
        {
            return "cells";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        //[NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName = null)//[CallerMemberName]
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
