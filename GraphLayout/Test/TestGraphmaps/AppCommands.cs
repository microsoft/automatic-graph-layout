using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TestGraphmaps
{
    public static class AppCommands
    {
         static readonly RoutedUICommand exitCommand = new RoutedUICommand("Exit application", "ExitCommand", typeof(AppCommands));

         static readonly RoutedUICommand updateViewCommand = new RoutedUICommand("Update view", "UpdateViewCommand", typeof(AppCommands));

         static readonly RoutedUICommand openFileCommand = new RoutedUICommand("Open File...", "OpenFileCommand",
                                                                             typeof(App));


         static readonly RoutedUICommand cancelLayoutCommand = new RoutedUICommand("Cancel Layout...", "CancelLayoutCommand",
                                                                                     typeof(App));

         static readonly RoutedUICommand reloadCommand = new RoutedUICommand("Reload File...", "ReloadCommand",
                                                                                   typeof(App));

         static readonly RoutedUICommand saveMsaglCommand = new RoutedUICommand("Save Msagl...", "SaveMsaglCommand",
                                                                                   typeof(App));

        //public static readonly RoutedUICommand ExitCommand = new RoutedUICommand("Exit...", "ExitCommand",
        //                                                                           typeof(App));

         static readonly RoutedUICommand homeViewCommand = new RoutedUICommand("Home view...", "HomeViewCommand",
                                                                                     typeof(App));

         static readonly RoutedUICommand scaleNodeDownCommand = new RoutedUICommand("Scale node down...",
                                                                                          "ScaleNodeDownCommand",
                                                                                          typeof(App));

         static readonly RoutedUICommand scaleNodeUpCommand = new RoutedUICommand("Scale node up...",
                                                                                        "ScaleNodeUpCommand",
                                                                                        typeof(App));

         static readonly RoutedUICommand setSelectCtrlPointsModeCommand = new RoutedUICommand("toggle SelectCtrlPointsMode",
                                                                                "SetSelectCtrlPointsModeCommand",
                                                                                typeof(App));

         static readonly RoutedUICommand updateAllNodeBoundingBoxesCommand = new RoutedUICommand("update all node bounding boxes",
                                                                        "UpdateAllNodeBoundingBoxes",
                                                                        typeof(App));

         static readonly RoutedUICommand createSkeletonBoxesAroundSelectedNodesCommand = new RoutedUICommand("Create Skeleton Boxes Around Selected Nodes",
                                                                "CreateSkeletonBoxesAroundSelectedNodes",
                                                                typeof(App));

         static readonly RoutedUICommand createSkeletonFromCdtCommand = new RoutedUICommand("Create Skeleton From Constrained Delaunay Triangulation",
                                                                "CreateSkeletonFromCdt",
                                                                typeof(App));

         static readonly RoutedUICommand createSkeletonGraphFromRailSegmentsCommand = new RoutedUICommand("Create Skeleton Graph From Rail Segments",
                                                                "CreateSkeletonGraphFromRailSegments",
                                                                typeof(App));

         static readonly RoutedUICommand selectAllNodesOnVisibleLevelsCommand = new RoutedUICommand("Select All Nodes On Visible Levels",
                                                                "SelectAllNodesOnVisibleLevels",
                                                                typeof(App));

         static readonly RoutedUICommand routeEdgesOnSkeletonCommand = new RoutedUICommand("Route Edges On Skeleton",
                                                        "RouteEdgesOnZeroLayer",
                                                        typeof(App));

         static readonly RoutedUICommand copySkeletonToNextLevelCommand = new RoutedUICommand("Copy Skeleton To Next Level",
                                                        "CopySkeletonToNextLevel",
                                                        typeof(App));

         static readonly RoutedUICommand removeUnusedSkeletonRailsCommand = new RoutedUICommand("Remove Unused Skeleton Rails",
                                                "RemoveUnusedSkeletonRails",
                                                typeof(App));

         static readonly RoutedUICommand testOverlapRemovalFixedSegments = new RoutedUICommand("Test OverlapRemovalFixedSegments",
                                                "TestOverlapRemovalFixedSegments",
                                                typeof(App));

         static readonly RoutedUICommand clearActiveSkeletonLevelCommand = new RoutedUICommand("Clear Active Skeleton Level",
                                                "ClearActiveSkeletonLevel",
                                                typeof(App));

         static readonly RoutedUICommand runOverlapRemovalBitmapCommand = new RoutedUICommand("Run Overlap Removal Bitmap",
                                                "RunOverlapRemovalBitmap",
                                                typeof(App));

         static readonly RoutedUICommand createSkeletonFromConeSpannerCommand = new RoutedUICommand("Create Skeleton From Cone Spanner",
                                                "CreateSkeletonFromConeSpanner",
                                                typeof(App));

         static readonly RoutedUICommand routeEdgesOnSkeletonTryKeepingOldTrajectoriesCommand = new RoutedUICommand("Route Edges On Skeleton, Try Keeping Old Trajectories",
                                                "RouteEdgesTryKeepingOldTrajectories",
                                                typeof(App));

         static readonly RoutedUICommand runGreedyNodeRailLevelCalculatorCommand = new RoutedUICommand("Run Greedy Node Rail Level Calculator",
                                        "RunGreedyNodeRailLevelCalculator",
                                        typeof(App));

         static readonly RoutedUICommand routeEdgesOnSkeletonNoAdditionalPortsCommand = new RoutedUICommand("Route Edges On Skeleton, No Additional Ports",
                                        "RouteEdgesOnSkeletonNoAdditionalPorts",
                                        typeof(App));

         static readonly RoutedUICommand refreshSidePanelCommand = new RoutedUICommand("Refresh Side Panel",
                                        "RefreshSidePanel",
                                        typeof(App));

         static readonly RoutedUICommand routeEdgesOnSkeletonNoPortsTryKeepingOldTrajectoriesCommand = new RoutedUICommand("Route Edges On Skeleton, No Ports, Try Keeping Old Trajectories",
                                        "RouteEdgesOnSkeletonNoPortsTryKeepingOldTrajectories",
                                        typeof(App));

         static readonly RoutedUICommand createSkeletonFromSteinerCdtCommand = new RoutedUICommand("Create Skeleton From Steiner Cdt",
                                        "CreateSkeletonFromSteinerCdt",
                                        typeof(App));

         static readonly RoutedUICommand magnifyNodesCommand = new RoutedUICommand("Magnify Nodes",
                                        "MagnifyNodes",
                                        typeof(App));

         static readonly RoutedUICommand scaleNodesBackCommand = new RoutedUICommand("Scale Nodes Back",
                                "ScaleNodesBack",
                                typeof(App));

         static readonly RoutedUICommand simplifyRoutesCommand = new RoutedUICommand("Simplify Routes",
                                "SimplifyRoutes",
                                typeof(App));

         static readonly RoutedUICommand simplifyRoutesAndUpdateCommand = new RoutedUICommand("Simplify Routes And Update",
                        "simplifyRoutesAndUpdate",
                        typeof(App));

         static readonly RoutedUICommand fillLevelWithNodesRoutesCommand = new RoutedUICommand("Fill Level With Nodes and Routes",
                        "FillLevelWithNodesRoutes",
                        typeof(App));

         static readonly RoutedUICommand shrinkNodesOfCurrentLevelCommand = new RoutedUICommand("Shrink Nodes Of Current Level",
                "ShrinkNodesOfCurrentLevel",
                typeof(App));

         static readonly RoutedUICommand copySkeletonFromPreviousLevelCommand = new RoutedUICommand("Copy Skeleton From Previous Level",
        "CopySkeletonFromPreviousLevel",
        typeof(App));

         static readonly RoutedUICommand doOneRunCommand = new RoutedUICommand("Do One Run", "DoOneRun", typeof(App));

         static readonly RoutedUICommand removeDegreeZeroCommand = new RoutedUICommand("Remove Degree Zero", "RemoveDegreeZero", typeof(App));

         
         static readonly RoutedUICommand runMdsCommand = new RoutedUICommand("Run Mds", "RunMds", typeof(App));

         static readonly RoutedUICommand magnifyNodesUniformlyLevelGeqCommand = new RoutedUICommand("Magnify Nodes Uniformly Level Geq", "MagnifyNodesUniformlyLevelGeq", typeof(App));

         static readonly RoutedUICommand takeScreenShotCommand = new RoutedUICommand("Take ScreenShot", "TakeScreenShot", typeof(App));

         static readonly RoutedUICommand generateTilesCommand = new RoutedUICommand("Generate Tiles", "GenerateTiles", typeof(App));
         
        static readonly RoutedUICommand showVisibleChildrenCountCommand = new RoutedUICommand("Show visible children count", "ShowVisibleChildrenCount", typeof(App));


        public static RoutedUICommand ExitCommand
        {
            get { return exitCommand; }
        }

        public static RoutedUICommand UpdateViewCommand
        {
            get { return updateViewCommand; }
        }

        public static RoutedUICommand OpenFileCommand
        {
            get { return openFileCommand; }
        }


        public static RoutedUICommand CancelLayoutCommand
        {
            get { return cancelLayoutCommand; }
        }

        public static RoutedUICommand ReloadCommand
        {
            get { return reloadCommand; }
        }

        
        public static RoutedUICommand SaveMsaglCommand
        {
            get { return saveMsaglCommand; }
        }

        public static RoutedUICommand HomeViewCommand
        {
            get { return homeViewCommand; }
        }

        public static RoutedUICommand ScaleNodeDownCommand
        {
            get { return scaleNodeDownCommand; }
        }

        public static RoutedUICommand ScaleNodeUpCommand
        {
            get { return scaleNodeUpCommand; }
        }

        public static RoutedUICommand SetSelectCtrlPointsModeCommand
        {
            get { return setSelectCtrlPointsModeCommand; }
        }

        public static RoutedUICommand UpdateAllNodeBoundingBoxesCommand
        {
            get { return updateAllNodeBoundingBoxesCommand; }
        }

        public static RoutedUICommand CreateSkeletonBoxesAroundSelectedNodesCommand
        {
            get { return createSkeletonBoxesAroundSelectedNodesCommand; }
        }

        public static RoutedUICommand CreateSkeletonFromCdtCommand
        {
            get { return createSkeletonFromCdtCommand; }
        }

        public static RoutedUICommand CreateSkeletonGraphFromRailSegmentsCommand
        {
            get { return createSkeletonGraphFromRailSegmentsCommand; }
        }

        public static RoutedUICommand SelectAllNodesOnVisibleLevelsCommand {
            get { return selectAllNodesOnVisibleLevelsCommand; }
        }

        public static RoutedUICommand RouteEdgesOnSkeletonCommand
        {
            get { return routeEdgesOnSkeletonCommand; }
        }

        public static RoutedUICommand CopySkeletonToNextLevelCommand
        {
            get { return copySkeletonToNextLevelCommand; }
        }

        public static RoutedUICommand RemoveUnusedSkeletonRailsCommand
        {
            get { return removeUnusedSkeletonRailsCommand; }
        }

        public static RoutedUICommand TestOverlapRemovalFixedSegments
        {
            get { return testOverlapRemovalFixedSegments; }
        }

        public static RoutedUICommand ClearActiveSkeletonLevelCommand
        {
            get { return clearActiveSkeletonLevelCommand; }
        }

        public static RoutedUICommand RunOverlapRemovalBitmapCommand
        {
            get { return runOverlapRemovalBitmapCommand; }
        }

        public static RoutedUICommand CreateSkeletonFromConeSpannerCommand
        {
            get { return createSkeletonFromConeSpannerCommand; }
        }

        public static RoutedUICommand RouteEdgesOnSkeletonTryKeepingOldTrajectoriesCommand
        {
            get { return routeEdgesOnSkeletonTryKeepingOldTrajectoriesCommand; }
        }

        public static RoutedUICommand RunGreedyNodeRailLevelCalculatorCommand
        {
            get { return runGreedyNodeRailLevelCalculatorCommand; }
        }

        public static RoutedUICommand RouteEdgesOnSkeletonNoAdditionalPortsCommand
        {
            get { return routeEdgesOnSkeletonNoAdditionalPortsCommand; }
        }

        public static RoutedUICommand RouteEdgesOnSkeletonNoPortsTryKeepingOldTrajectoriesCommand
        {
            get { return routeEdgesOnSkeletonNoPortsTryKeepingOldTrajectoriesCommand; }
        }

        public static RoutedUICommand RefreshSidePanelCommand
        {
            get { return refreshSidePanelCommand; }
        }

        public static RoutedUICommand CreateSkeletonFromSteinerCdtCommand
        {
            get { return createSkeletonFromSteinerCdtCommand; }
        }

        public static RoutedUICommand MagnifyNodesCommand
        {
            get { return magnifyNodesCommand; }
        }

        public static RoutedUICommand ScaleNodesBackCommand
        {
            get { return scaleNodesBackCommand; }
        }

        public static RoutedUICommand SimplifyRoutesCommand
        {
            get { return simplifyRoutesCommand; }
        }

        public static RoutedUICommand SimplifyRoutesAndUpdateCommand
        {
            get { return simplifyRoutesAndUpdateCommand; }
        }

        public static RoutedUICommand FillLevelWithNodesRoutesCommand
        {
            get { return fillLevelWithNodesRoutesCommand; }
        }

        public static RoutedUICommand ShrinkNodesOfCurrentLevelCommand
        {
            get { return shrinkNodesOfCurrentLevelCommand; }
        }

        public static RoutedUICommand CopySkeletonFromPreviousLevelCommand
        {
            get { return copySkeletonFromPreviousLevelCommand; }
        }

        public static RoutedUICommand DoOneRunCommand
        {
            get { return doOneRunCommand; }
        }

        public static RoutedUICommand RemoveDegreeZeroCommand
        {
            get { return removeDegreeZeroCommand; }
        }

        
        public static RoutedUICommand RunMdsCommand
        {
            get { return runMdsCommand; }
        }

        public static RoutedUICommand MagnifyNodesUniformlyLevelGeqCommand
        {
            get { return magnifyNodesUniformlyLevelGeqCommand; }
        }

        public static RoutedUICommand TakeScreenShotCommand
        {
            get { return takeScreenShotCommand; }
        }

        public static RoutedUICommand GenerateTilesCommand
        {
            get { return generateTilesCommand; }
        }

        public static RoutedUICommand ShowVisibleChildrenCountCommand {
            get { return showVisibleChildrenCountCommand; }
        }
    }
}
