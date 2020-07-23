using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Cache;
using System.Runtime.Remoting.Services;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Miscellaneous.RegularGrid;
using Triple = System.Tuple<int,int,int>;
using Timer=Microsoft.Msagl.DebugHelpers.Timer;
namespace Microsoft.Msagl.GraphmapsWpfControl {
    internal class TileFetcher {
        const double TileUpdateIntervalMillisecods = 200;
        DispatcherTimer _timer;

        const int MaxImagesToCache = 200;
        readonly Queue<Triple> _cachedQueue = new Queue<Triple>();
        Dictionary<Triple, BitmapImage> _cachedImages = new Dictionary<Triple, BitmapImage>();
        readonly GraphmapsViewer _graphViewer;
        readonly Dictionary<Triple, Image> _activeImages = new Dictionary<Triple, Image>();
        CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        readonly object _cacheLock = new object();
        readonly Func<Set<Triple>> _desiredActiveSetFunc;
        double _scale, _xOffset, _yOffset;
        public TileFetcher(GraphmapsViewer graphViewer, Func<Set<Triple>> desiredActiveSetFunc) {
            _graphViewer = graphViewer;
            _desiredActiveSetFunc = desiredActiveSetFunc;
            SetupTilesDispatcherTimer();
        }

        void SetupTilesDispatcherTimer() {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(TileUpdateIntervalMillisecods),
                IsEnabled = false
            };
            _timer.Tick += (s, e) =>
                {
                    if (ChangeInTransform()) {
                        _cancellationTokenSource.Cancel(); // just in case the thread is running
                        _cancellationTokenSource = new CancellationTokenSource();
                        return;
                    }
                    UpdateTiles();
                };
        }

        bool ChangeInTransform() {
            bool ret = false;
            var matrix = ((MatrixTransform)_graphViewer.GraphCanvas.RenderTransform).Matrix;
            if (_scale != matrix.M11) {
                ret = true;
                _scale = matrix.M11;
            }
            if (_xOffset != matrix.OffsetX) {
                _xOffset = matrix.OffsetX;
                ret = true;
            }
            if (_yOffset != matrix.OffsetY) {
                _yOffset = matrix.OffsetY;
                ret = true;
            }
            return ret;
        }

        internal void StartLoadindTiles() {
            InitTransform();           
            _timer.Stop();
            _timer.Start();
        }

        void InitTransform() {
            _scale = _xOffset = _yOffset = 33; // just to start with some values
        }

        internal void UpdateTiles() {
            lock (this) {
                var grid = new GridTraversal(_graphViewer.Graph.BoundingBox, _graphViewer.GetBackgroundTileLevel());
                Set<Triple> desiredActiveSetOfImages = _desiredActiveSetFunc();
                Set<Triple> toRemove = new Set<Triple>(_activeImages.Keys) - desiredActiveSetOfImages;
                Task.Factory.StartNew(() =>
                    {
                        foreach (var visibleTileKey in desiredActiveSetOfImages)
                            CreateAndCacheBitmapIfNeeded(visibleTileKey, grid);
                    },
                    _cancellationTokenSource.Token,
                    TaskCreationOptions.None,
                    TaskScheduler.Default).
                    ContinueWith(t =>
                        {
                            foreach (var visibleTileKey in desiredActiveSetOfImages)
                                DisplayBitmap(visibleTileKey, grid);

                            _graphViewer.GraphCanvas.Dispatcher.Invoke(() => RemoveTiles(toRemove));
                            _timer.Stop(); // we are done
                        }, TaskContinuationOptions.OnlyOnRanToCompletion);
            }
        }


        void RemoveTiles(Set<Triple> toRemove) {
            foreach (var triple in toRemove) {
                lock (_cacheLock) {
                    if (!_activeImages.ContainsKey(triple)) continue;
                    _graphViewer.GraphCanvas.Children.Remove(_activeImages[triple]);
                    _activeImages.Remove(triple);
                }
            }
        }

        void DisplayBitmap(Triple triple, GridTraversal grid) {
            _graphViewer.GraphCanvas.Dispatcher.Invoke(() => {
                lock (_cacheLock) {
                    if (_activeImages.ContainsKey(triple)) return;
                    BitmapImage sourceBitmapImage;
                    if (!_cachedImages.TryGetValue(triple, out sourceBitmapImage)) return;
                    var image = new Image {Source = sourceBitmapImage, Tag = triple};
                    _activeImages[triple] = image;
                    _graphViewer.GraphCanvas.Children.Add(image);

                    image.Width = grid.TileWidth;
                    image.Height = grid.TileHeight;
                    var center = grid.GetTileCenter(triple.Item2, triple.Item3);
                    Common.PositionFrameworkElement(image, center, 1);
                }
            });
        }

        void CreateAndCacheBitmapIfNeeded(Triple triple, GridTraversal grid) {
            if (_activeImages.ContainsKey(triple)) return;
            if (_cachedImages.ContainsKey(triple)) return;
            var fname = _graphViewer.CreateTileFileName(triple.Item2, triple.Item3, grid);
            if (!File.Exists(fname)) return;
            WpfMemoryPressureHelper.ResetTimers();
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            bitmapImage.CacheOption = BitmapCacheOption.None;
            bitmapImage.UriSource = new Uri(_graphViewer.CreateTileFileName(triple.Item2, triple.Item3, grid));
            bitmapImage.UriCachePolicy=new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            CacheBitmap(triple, bitmapImage);
        }

        void CacheBitmap(Triple triple, BitmapImage bitmapImage) {
            lock (_cacheLock) {
                if (_cachedImages.ContainsKey(triple))
                    return;
                _cachedImages[triple] = bitmapImage;
                if (_cachedQueue.Count >= MaxImagesToCache)
                    _cachedImages.Remove(_cachedQueue.Dequeue());
                _cachedQueue.Enqueue(triple);
            }
        }

        public void Clear() {
            _cachedImages.Clear();
            _cachedQueue.Clear();
            _activeImages.Clear();
            InitTransform();
        }
    }
}