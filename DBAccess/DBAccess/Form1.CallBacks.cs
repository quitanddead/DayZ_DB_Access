﻿using BattleNET;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace DBAccess
{
    public partial class MainWindow : Form
    {
        public delegate void DelegateVoid();

        #region Fields
        private static EventWaitHandle eventMapZoomBgWorker = new EventWaitHandle(false, EventResetMode.AutoReset);
        private static Mutex mtxTileRequest = new Mutex();
        private static Mutex mtxTileUpdate = new Mutex();
        private DelegateVoid dlgUpdateIcons;
        private DelegateVoid dlgRefreshMap;
        //
        private static Dictionary<Int32, bool> dicTileExistence = new Dictionary<Int32, bool>();
        private static bool TileFileExists(tileReq req)
        {
            Int32 key = req.Key;
            if (dicTileExistence.ContainsKey(key))
                return dicTileExistence[key];

            dicTileExistence[key] = File.Exists(req.path);
            return dicTileExistence[key];
        }
        private bool IsPlayerOnline(string uid)
        {
            DataRow rowAlive = myDB.PlayersAlive.Tables[0].Rows.Find(uid);
            if (rowAlive != null)
            {
                DataRow rowOnline = PlayerNamesOnline.Tables[0].Rows.Find(rowAlive.Field<string>("name"));
                if (rowOnline != null)
                    return (rowOnline.Field<string>("Status") == "Ingame");

                return false;
            }

            return false;
        }
        #endregion

        #region ToolStrip
        private void toolStripMenuItemAddVehicle_Click(object sender, EventArgs e)
        {
            if (dataGridViewVehicleTypes.SelectedCells.Count == 1)
            {
                var row = dataGridViewVehicleTypes.Rows[dataGridViewVehicleTypes.SelectedCells[0].RowIndex];
                string classname = row.Cells["ColGVVTClassName"].Value as string;

                var rowT = mySrvCfg.vehicle_types.Tables[0].Rows.Find(classname);
                if (rowT != null)
                {
                    var vehicle_id = rowT.Field<UInt16>("Id");

                    bool bRes = myDB.AddVehicle((currentMode == displayMode.ShowSpawn), classname, vehicle_id, positionInDB);
                    if (bRes)
                    {
                        RefreshDB();
                    }
                    else
                    {
                        MessageBox.Show("Error while trying to insert vehicle instance '" + classname + "' into database");
                    }
                }
            }
        }
        private void toolStripMenuItemTeleportVehicle_Click(object sender, EventArgs e)
        {
            var vehicle = propertyGrid1.SelectedObject as Vehicle;
            if (vehicle != null)
            {
                bool bRes = myDB.TeleportVehicle(vehicle.uid.ToString(), positionInDB);
                if (bRes)
                {
                    RefreshDB();
                }
                else
                {
                    MessageBox.Show("Error while trying to teleport vehicle '" + vehicle.uid + "' into database");
                }
            }
        }
        private void toolStripMenuItemTeleportPlayer_Click(object sender, EventArgs e)
        {
            var survivor = propertyGrid1.SelectedObject as Survivor;
            if(survivor != null)
            {
                bool bRes = myDB.TeleportPlayer(survivor.uid, positionInDB);
                if (bRes)
                {
                    RefreshDB();
                }
                else
                {
                    MessageBox.Show("Error while trying to teleport player '" + survivor.name + "' into database");
                }
            }
        }
        private void cb_toolStripMenuItemHealPlayer_Click(object sender, EventArgs e)
        {
            var survivor = propertyGrid1.SelectedObject as Survivor;
            if (survivor != null)
            {
                if (IsPlayerOnline(survivor.uid) == false)
                {
                    bool bRes = myDB.HealPlayer(survivor.uid);
                    if (bRes)
                    {
                        RefreshDB();
                    }
                    else
                    {
                        MessageBox.Show("Error while trying to heal player '" + survivor.name + "' into database");
                    }
                }
                else
                {
                    MessageBox.Show("Player '" + survivor.name + "' must be offline or in lobby");
                }
            }
        }
        private void cb_toolStripMenuItemRevivePlayer_Click(object sender, EventArgs e)
        {
            var survivor = propertyGrid1.SelectedObject as Survivor;
            if (survivor != null)
            {
                if (IsPlayerOnline(survivor.uid) == false)
                {
                    bool bRes = myDB.RevivePlayer(survivor.uid, survivor.idb.row.Field<uint>("id").ToString());
                    if (bRes)
                    {
                        RefreshDB();
                    }
                    else
                    {
                        MessageBox.Show("Error while trying to revive player '" + survivor.name + "' into database");
                    }
                }
                else
                {
                    MessageBox.Show("Player '" + survivor.name + "' must be offline or in lobby");
                }
            }
        }
        private void cb_toolStripMenuItemSavePlayerState_Click(object sender, EventArgs e)
        {
            var survivor = propertyGrid1.SelectedObject as Survivor;
            if (survivor != null)
            {
                if (IsPlayerOnline(survivor.uid) == false)
                {
                    bool bRes = myDB.SavePlayerState(survivor.uid);
                    if (!bRes)
                    {
                        MessageBox.Show("Error while trying to save player '" + survivor.name + "' state from database");
                    }
                }
                else
                {
                    MessageBox.Show("Player '" + survivor.name + "' must be offline or in lobby");
                }
            }
        }
        private void cb_toolStripMenuItemRestorePlayerState_Click(object sender, EventArgs e)
        {
            var survivor = propertyGrid1.SelectedObject as Survivor;
            if (survivor != null)
            {
                if (IsPlayerOnline(survivor.uid) == false)
                {
                    bool bRes = myDB.RestorePlayerState(survivor.uid);
                    if (bRes)
                    {
                        RefreshDB();
                    }
                    else
                    {
                        MessageBox.Show("Error while trying to restore player '" + survivor.name + "' state in database");
                    }
                }
                else
                {
                    MessageBox.Show("Player '" + survivor.name + "' must be offline or in lobby");
                }
            }
        }
        private void toolStripStatusMapHelper_Click(object sender, EventArgs e)
        {
            currentMode = displayMode.MapHelper;
        }
        private void cb_toolStripStatusDeployable_Click(object sender, EventArgs e)
        {
            currentMode = displayMode.ShowDeployable;
        }
        private void toolStripStatusSpawn_Click(object sender, EventArgs e)
        {
            currentMode = displayMode.ShowSpawn;
        }
        private void toolStripStatusTraders_Click(object sender, EventArgs e)
        {
            currentMode = displayMode.ShowTraders;
        }
        private void toolStripStatusVehicle_Click(object sender, EventArgs e)
        {
            currentMode = displayMode.ShowVehicle;
        }
        private void toolStripStatusAlive_Click(object sender, EventArgs e)
        {
            currentMode = displayMode.ShowAlive;
        }
        private void toolStripStatusDead_Click(object sender, EventArgs e)
        {
            currentMode = displayMode.ShowDead;
        }
        private void toolStripStatusOnline_Click(object sender, EventArgs e)
        {
            currentMode = displayMode.ShowOnline;
        }
        private void toolStripStatusWorld_Click(object sender, EventArgs e)
        {
            currentMode = displayMode.SetMaps;
            
            SelectBitmap();
            SetCurrentMap();
        }
        private void cb_toolStripStatusTrail_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                bShowTrails = !bShowTrails;

                toolIconTrails.Select = bShowTrails;
                //toolStripStatusTrail.BorderSides = (bShowTrails) ? ToolStripStatusLabelBorderSides.All : ToolStripStatusLabelBorderSides.None;
            }
            else if (e.Button == MouseButtons.Right)
            {
                foreach (KeyValuePair<string, UIDGraph> pair in dicUIDGraph)
                    pair.Value.ResetPaths();
            }
        }
        private void toolStripStatusNews_Click(object sender, EventArgs e)
        {
            diagAbout.ShowDialog();
        }
        private void toolStripStatusHelp_Click(object sender, EventArgs e)
        {
            // Show/Hide chat panel
            splitContainerGlobal.Panel2Collapsed = !splitContainerGlobal.Panel2Collapsed;

            toolIconChat.Select = !splitContainerGlobal.Panel2Collapsed;
            //toolStripStatusChat.BorderSides = (!splitContainerGlobal.Panel2Collapsed) ? ToolStripStatusLabelBorderSides.All : ToolStripStatusLabelBorderSides.None;
        }
        #endregion

        #region MainForm
        private void cb_Form1Resize(object sender, EventArgs e)
        {
            ApplyMapChanges();
        }
        private void cb_Form1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (!virtualMap.Enabled)
                return;

            mapZoom.Start(virtualMap,
                          (Tool.Point)(e.Location - virtualMap.Position),
                          Math.Sign(e.Delta));
        }
        private void cb_Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // Cancel background workers
                bgWorkerBattlEye.CancelAsync();
                bgWorkerDatabase.CancelAsync();
                bgWorkerMapZoom.CancelAsync();
                bgWorkerLoadTiles.CancelAsync();
                bgWorkerFocus.CancelAsync();
                bgWorkerRefreshLeds.CancelAsync();

                // Will release the bgWorkerMapZoom thread
                eventMapZoomBgWorker.Set();

                SaveConfigFiles();

                CloseConnection();

                // Wait until background workers are down
                while (bgWorkerBattlEye.IsBusy || bgWorkerDatabase.IsBusy || bgWorkerMapZoom.IsBusy || bgWorkerLoadTiles.IsBusy || bgWorkerFocus.IsBusy || bgWorkerRefreshLeds.IsBusy)
                {
                    Application.DoEvents();
                    Thread.Sleep(125);
                }
            }
            catch
            {
            }
        }
        #endregion

        #region MapPanel
        private void cb_splitContainer1_Panel1_SizeChanged(object sender, EventArgs e)
        {
            var splitter = sender as SplitterPanel;

            panelCnx.Location = new Point((splitter.Width - panelCnx.Width) / 2,
                                         (splitter.Height - panelCnx.Height) / 2);
        }
        private void cb_contextMenuStripItemPlayerMenu_Opening(object sender, CancelEventArgs e)
        {
            e.Cancel = false;

            var survivor = propertyGrid1.SelectedObject as Survivor;
            if (survivor != null)
            {
                bool playerIsOffline = IsPlayerOnline(survivor.uid) == false;

                toolStripMenuItemHeal.Text = "Heal player '" + survivor.name + ((playerIsOffline) ? "'" : "' must be offline or in lobby");

                toolStripMenuItemHeal.Enabled = playerIsOffline;
                toolStripMenuItemSavePlayerState.Enabled = playerIsOffline;
                toolStripMenuItemRestorePlayerState.Enabled = playerIsOffline && (mySrvCfg.player_state.Tables.Count > 0) && (mySrvCfg.player_state.Tables[0].Rows.Find(survivor.uid) != null);
            }
            else
            {
                toolStripMenuItemHeal.Text = "Heal player: No survivor selected";
                toolStripMenuItemHeal.Enabled = false;
                toolStripMenuItemSavePlayerState.Enabled = false;
                toolStripMenuItemRestorePlayerState.Enabled = false;
            }
        }
        private void contextMenuStripItemMenu_Opening(object sender, CancelEventArgs e)
        {
            e.Cancel = false;

            switch (currentMode)
            {
                case displayMode.ShowAlive:
                case displayMode.ShowOnline:
                    {
                        var survivor = propertyGrid1.SelectedObject as Survivor;

                        if (survivor != null)
                        {
                            if (IsPlayerOnline(survivor.uid) == false)
                            {
                                toolStripMenuMapTeleportPlayer.Text = "Teleport player '" + survivor.name + "'";
                                toolStripMenuMapTeleportPlayer.Enabled = true;
                            }
                            else
                            {
                                toolStripMenuMapTeleportPlayer.Text = "Teleport: player '" + survivor.name + "' must be offline or in lobby";
                                toolStripMenuMapTeleportPlayer.Enabled = false;
                            }
                        }
                        else
                        {
                            toolStripMenuMapTeleportPlayer.Text = "Teleport: No survivor selected";
                            toolStripMenuMapTeleportPlayer.Enabled = false;
                        }
                        contextMenuStripMapMenu.Items.Clear();
                        contextMenuStripMapMenu.Items.Add(toolStripMenuMapTeleportPlayer);
                    }
                    break;

                case displayMode.ShowVehicle:
                case displayMode.ShowSpawn:
                    {
                        e.Cancel = true;

                        if (currentMode == displayMode.ShowVehicle)
                        {
                            var vehicle = propertyGrid1.SelectedObject as Vehicle;

                            if (vehicle != null)
                            {
                                toolStripMenuMapTeleportVehicle.Text = "Teleport vehicle '" + vehicle.uid.ToString() + "'";
                                toolStripMenuMapTeleportVehicle.Enabled = true;
                            }
                            else
                            {
                                toolStripMenuMapTeleportVehicle.Text = "Teleport: No vehicle selected";
                                toolStripMenuMapTeleportVehicle.Enabled = false;
                            }
                            contextMenuStripMapMenu.Items.Clear();
                            contextMenuStripMapMenu.Items.Add(toolStripMenuMapTeleportVehicle);
                            e.Cancel = false;
                        }

                        if (!IsEpochSchema)
                        {
                            if (dataGridViewVehicleTypes.SelectedCells.Count == 1)
                            {
                                var row = dataGridViewVehicleTypes.Rows[dataGridViewVehicleTypes.SelectedCells[0].RowIndex];
                                string sType = "";
                                if (currentMode == displayMode.ShowSpawn)
                                    sType = "Spawnpoint";

                                toolStripMenuMapAddVehicle.Text = "Add " + row.Cells["ColGVVTType"].Value + " '" + row.Cells["ColGVVTClassName"].Value + "' " + sType;
                                toolStripMenuMapAddVehicle.Enabled = true;
                            }
                            else
                            {
                                toolStripMenuMapAddVehicle.Text = "Add vehicle: Select a vehicle from tab 'Vehicles'";
                                toolStripMenuMapAddVehicle.Enabled = false;
                            }
                            contextMenuStripMapMenu.Items.Clear();
                            contextMenuStripMapMenu.Items.Add(toolStripMenuMapAddVehicle);
                            e.Cancel = false;
                        }
                    }
                    break;

                default:
                    e.Cancel = true;
                    break;
            }
        }
        private void cb_Panel1_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                if (virtualMap.Enabled)
                {
                    e.Graphics.CompositingMode = CompositingMode.SourceCopy;
                    //e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
                    e.Graphics.InterpolationMode = gfxIntplMode;
                    e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;

                    mtxTileUpdate.WaitOne();
                    int nb_tilesDrawn = 0;
                    foreach (tileReq req in tileRequests)
                    {
                        if (req.bDontDisplay == false)
                        {
                            tileNfo nfo = tileCache.Find(x => req.path == x.path);

                            if (nfo != null)
                            {
                                //  Display tile
                                e.Graphics.DrawImage(nfo.bitmap, req.rec);
                                nb_tilesDrawn++;
                            }
                            else
                            {
                                // Display an ancestor instead
                                int hdepth = req.depth;
                                int hx = req.x;
                                int hy = req.y;
                                int hpow = 1;

                                while (hdepth > virtualMap.nfo.min_depth)
                                {
                                    // Try to display the closest ancestor
                                    hdepth--;
                                    hx /= 2;
                                    hy /= 2;
                                    hpow *= 2;
                                    string fpath = virtualMap.nfo.tileBasePath + hdepth + "\\Tile" + hy.ToString("000") + hx.ToString("000") + ".jpg";
                                    nfo = tileCache.Find(x => fpath == x.path);
                                    if (nfo != null)
                                    {
                                        nfo.ticks = DateTime.Now.Ticks;
                                        int width = nfo.bitmap.Width / hpow;
                                        int height = nfo.bitmap.Height / hpow;
                                        int x = (req.x % hpow) * width;
                                        int y = (req.y % hpow) * height;
                                        Rectangle recSrc = new Rectangle(x, y, width, height);
                                        e.Graphics.DrawImage(nfo.bitmap, req.rec, recSrc, GraphicsUnit.Pixel);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    mtxTileUpdate.ReleaseMutex();

                    e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                    e.Graphics.CompositingMode = CompositingMode.SourceOver;

                    if (!IsMapHelperEnabled)
                    {
                        if (bShowTrails == true)
                        {
                            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                            foreach (iconDB idb in listIcons)
                            {
                                if ((idb.type == UIDType.TypePlayer) || (idb.type == UIDType.TypeVehicle))
                                    GetUIDGraph(idb.uid).DisplayOnMap(e.Graphics, virtualMap);
                            }
                        }

                        e.Graphics.SmoothingMode = SmoothingMode.None;
                        Rectangle recPanel = new Rectangle(Point.Empty, splitContainer1.Panel1.Size);

                        foreach (iconDB idb in listIcons)
                        {
                            if (recPanel.IntersectsWith(idb.icon.rectangle))
                            {
                                System.Drawing.Imaging.ImageAttributes attrib = (selectedIcon == idb) ? attrSelected : attrUnselected/*GetPlayerColor(idb.uid)*/;
                                e.Graphics.DrawImage(idb.icon.image, idb.icon.rectangle, 0, 0, idb.icon.image.Width, idb.icon.image.Height, GraphicsUnit.Pixel, attrib);
                            }
                        }
                    }
                    else
                    {
                        mapHelper.Display(e.Graphics);
                    }

                    if (!IsMapHelperEnabled && bCartographer)
                        cartographer.DisplayOnMap(e.Graphics, virtualMap);

                    //---- Show item info ----
                    {
                        if(hoverIcon != null && hoverIcon.type == UIDType.TypePlayer)
                        {
                            string name = hoverIcon.row.Field<string>("Name");
                            e.Graphics.CompositingMode = CompositingMode.SourceOver;
                            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                            Font ft = new Font("Buxton Sketch", 16, FontStyle.Bold);
                            Point mousePos = splitContainer1.Panel1.PointToClient(Cursor.Position);

                            e.Graphics.DrawString(name, ft, Brushes.Black, mousePos + new Size(12, 7));
                            e.Graphics.DrawString(name, ft, Brushes.White, mousePos + new Size(10, 5));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception found");
            }
        }
        private void cb_Panel1_MouseClick(object sender, MouseEventArgs e)
        {
            System.Threading.Interlocked.CompareExchange(ref bUserAction, 1, 0);

            if (IsMapHelperEnabled)
                return;

            if (bCartographer)
            {
                if (e.Button.HasFlag(MouseButtons.Left))
                {
                    Tool.Point mp = virtualMap.UnitToDB(virtualMap.PanelToUnit(e.Location));
                    mp = mp / virtualMap.nfo.dbRefMapSize;
                    mp.Y = 1.0f - mp.Y;
                    cartographer.AddPoint(mp);
                }
                else if (e.Button.HasFlag(MouseButtons.Right))
                {
                    cartographer.RemoveLastPoint();
                }
                else if (e.Button.HasFlag(MouseButtons.Middle))
                {
                    cartographer.AddPoint(new Tool.Point(0, 1));
                }
            }
            else
            {
                selectedIcon = null;
                Rectangle mouseRec = new Rectangle(e.Location, Size.Empty);

                // Call Click() on (lastly) selected icon
                foreach (iconDB idb in listIcons)
                {
                    if (mouseRec.IntersectsWith(idb.icon.rectangle))
                        selectedIcon = idb;
                }
                if (selectedIcon != null)
                {
                    // Call Click event from icon
                    selectedIcon.icon.OnClick(this, e);
                    splitContainer1.Panel1.Invalidate();
                }
                if ((selectedIcon == null) && e.Button.HasFlag(MouseButtons.Right))
                {
                    contextMenuStripMapMenu.Show(this, e.Location);
                }
            }
        }
        private void cb_Panel1_MouseDown(object sender, MouseEventArgs e)
        {
            System.Threading.Interlocked.CompareExchange(ref bUserAction, 1, 0);

            if (e.Button.HasFlag(MouseButtons.Right) && IsMapHelperEnabled)
            {
                Tool.Point mousePos = (Tool.Point)(e.Location - virtualMap.Position);

                mapHelper.isDraggingCtrlPoint = mapHelper.IntersectControl(mousePos, 5);

                if (mapHelper.isDraggingCtrlPoint > 0)
                {
                    // Will drag selected Control point
                    Tool.Point pt = mapHelper.controls[mapHelper.isDraggingCtrlPoint] * virtualMap.SizeCorrected + virtualMap.Position;
                    mapPan.Start(pt);
                }
                else
                {
                    // Will drag all Control points
                    List<Tool.Point> points = new List<Tool.Point>(4);
                    for (int i = 0; i < 4; i++)
                    {
                        Tool.Point pt = mapHelper.controls[i] * virtualMap.SizeCorrected + virtualMap.Position;
                        points.Add(pt);
                    }
                    mapPan.Start(points);
                }

                splitContainer1.Panel1.Invalidate();
            }
            else
            {
                mapPan.Start(virtualMap.Position);
            }
        }
        private void cb_Panel1_MouseMove(object sender, MouseEventArgs e)
        {
            Rectangle recPanel = new Rectangle(Point.Empty, splitContainer1.Panel1.Size);
            Rectangle recMouse = new Rectangle(e.Location, Size.Empty);

            if (!recPanel.IntersectsWith(recMouse))
                return;

            if(myDB.Connected)
                splitContainer1.Panel1.Focus();

            if (e.Button.HasFlag(MouseButtons.Right) && (mapHelper != null))
            {
                if (mapHelper.enabled)
                {
                    if (mapPan.IsStarted)
                    {
                        mapPan.Update();

                        if (mapHelper.isDraggingCtrlPoint >= 0)
                        {
                            Tool.Point newPos = mapPan.Position(0);
                            Tool.Point pt = (Tool.Point)((newPos - virtualMap.Position) / virtualMap.SizeCorrected);

                            mapHelper.controls[mapHelper.isDraggingCtrlPoint] = pt;
                            mapHelper.ControlPointUpdated(mapHelper.isDraggingCtrlPoint);
                        }
                        else
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                Tool.Point newPos = mapPan.Position(i);
                                Tool.Point pt = (Tool.Point)((newPos - virtualMap.Position) / virtualMap.SizeCorrected);

                                mapHelper.controls[i] = pt;
                            }
                            mapHelper.ControlPointUpdated(0);
                        }

                        ApplyMapChanges();
                    }
                }
            }
            else if (e.Button.HasFlag(MouseButtons.Left))
            {
                if (mapPan.IsStarted)
                {
                    mapPan.Update();
                    virtualMap.Position = mapPan.Position(0);
                    ApplyMapChanges();
                }
            }
            else
            {
                if (IsMapHelperEnabled)
                {
                    Tool.Point mousePos = (Tool.Point)(e.Location - virtualMap.Position);
                    mapHelper.isDraggingCtrlPoint = mapHelper.IntersectControl(mousePos, 5);
                    splitContainer1.Panel1.Invalidate();
                }
                else
                {
                    if (listIcons.Count > 0)
                    {
                        Tool.Point mouseUnit = virtualMap.PanelToUnit(e.Location);
                        mouseUnit.Y = 1 - mouseUnit.Y;

                        iconDB nearest = null;
                        float nearestLength = float.MaxValue;
                        foreach (iconDB idb in listIcons)
                        {
                            float length = (mouseUnit - idb.pos).Lenght;
                            if ((nearest == null) || (nearestLength > length))
                            {
                                nearest = idb;
                                nearestLength = length;
                            }
                        }

                        if (nearestLength * 1000 > (50 / (float)(Math.Pow(2, virtualMap.Depth))))
                            nearest = null;

                        if (prevHoverIcon != nearest)
                        {
                            if (prevHoverIcon != null)
                                prevHoverIcon.icon.rectangle = new Rectangle(prevHoverIcon.icon.rectangle.Location + (Tool.Size)prevHoverIcon.icon.image.Size * 0.5f,
                                                                           (Tool.Size)prevHoverIcon.icon.image.Size);

                            if (nearest != null)
                                nearest.icon.rectangle = new Rectangle(nearest.icon.rectangle.Location - (Tool.Size)nearest.icon.image.Size * 0.5f,
                                                                       (Tool.Size)nearest.icon.image.Size * 2);

                            splitContainer1.Panel1.Invalidate();
                            prevHoverIcon = nearest;

                            if (nearest != null && listIcons[listIcons.Count - 1] != nearest)
                            {
                                listIcons.Remove(nearest);
                                listIcons.Add(nearest);
                            }
                            hoverIcon = nearest;
                        }
                    }
                }

                mapPan.Stop();
            }
            /*
            if (!IsMapHelperEnabled && bCartographer)
            {
                string pathstr = "public static Point[] ptsXXX = new Point[]\r\n{";
                foreach (PathDef def in cartographer.paths)
                {
                    foreach (Tool.Point pt in def.points)
                    {
                        Tool.Point npt = pt;
                        npt.Y = 1.0f - npt.Y;
                        npt = npt * virtualMap.nfo.dbRefMapSize;
                        pathstr += "\r\nnew Point" + npt.ToStringInt() + ",";
                    }
                }
                pathstr = pathstr.TrimEnd(',');
                pathstr += "\r\n};";
                textBoxCmdStatus.Text = pathstr;
            }
            */
            // Database coordinates
            positionInDB = virtualMap.UnitToDB(virtualMap.PanelToUnit(e.Location));
            // Map coordinates
            Tool.Point mp = virtualMap.UnitToMap(virtualMap.PanelToUnit(e.Location));

            if ((mp.X > -100000) && (mp.X < 100000))
            {
                toolStripStatusCoordDB.Text = ((int)positionInDB.X).ToString() + "\n" + ((int)positionInDB.Y).ToString();
                toolStripStatusCoordMap.Text = ((int)mp.X).ToString() + "\n" + ((int)mp.Y).ToString();
            }
        }
        private void cb_Panel1_MouseUp(object sender, MouseEventArgs e)
        {
            if (mapHelper != null)
                mapHelper.isDraggingCtrlPoint = -1;

            System.Threading.Interlocked.CompareExchange(ref bUserAction, 0, 1);
        }
        private void OnPlayerClick(object sender, EventArgs e)
        {
            myIcon pb = sender as myIcon;

            Survivor player = new Survivor(pb.iconDB);
            player.Rebuild();
            propertyGrid1.SelectedObject = player;
            propertyGrid1.ExpandAllGridItems();
        }
        private void OnVehicleClick(object sender, EventArgs e)
        {
            myIcon pb = sender as myIcon;

            if (pb.iconDB.type == UIDType.TypeVehicle)
            {
                Vehicle vehicle = new Vehicle(pb.iconDB);
                vehicle.Rebuild();
                propertyGrid1.SelectedObject = vehicle;
                propertyGrid1.ExpandAllGridItems();
            }
            else
            {
                Spawn spawn = new Spawn(pb.iconDB);
                spawn.Rebuild();
                propertyGrid1.SelectedObject = spawn;
                propertyGrid1.ExpandAllGridItems();
            }

            // Select class name in Vehicles table
            foreach (DataGridViewRow row in dataGridViewVehicleTypes.Rows)
            {
                if (row.Cells["ColGVVTClassName"].Value as string == pb.iconDB.row.Field<string>("class_name"))
                    row.Cells["ColGVVTType"].Selected = true;
            }
        }
        private void OnDeployableClick(object sender, EventArgs e)
        {
            myIcon pb = sender as myIcon;

            Deployable deployable = new Deployable(pb.iconDB);
            deployable.Rebuild();
            propertyGrid1.SelectedObject = deployable;
            propertyGrid1.ExpandAllGridItems();

            // Select class name in Deployables table
            foreach (DataGridViewRow row in dataGridViewDeployableTypes.Rows)
            {
                if (row.Cells["ColGVDTClassName"].Value as string == pb.iconDB.row.Field<string>("class_name"))
                    row.Cells["ColGVDTType"].Selected = true;
            }
        }
        private void cb_repairRefuelVehicleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (selectedIcon != null)
            {
                if (myDB.RepairAndRefuelVehicle(selectedIcon.uid))
                {
                    RefreshDB();
                    textBoxCmdStatus.Text = "repaired & refueled vehicle id " + selectedIcon.uid;
                }
            }
            selectedIcon = null;
        }
        private void toolStripMenuItemDelete_Click(object sender, EventArgs e)
        {
            if (selectedIcon != null)
            {
                if (myDB.DeleteVehicle(selectedIcon.uid))
                {
                    RefreshDB();
                    textBoxCmdStatus.Text = "removed vehicle id " + selectedIcon.uid;
                }
            }
            selectedIcon = null;
        }
        private void toolStripMenuItemDeleteSpawn_Click(object sender, EventArgs e)
        {
            if (selectedIcon != null)
            {
                if (myDB.DeleteSpawn(selectedIcon.uid))
                {
                    RefreshDB();
                    textBoxCmdStatus.Text = "removed vehicle spawnpoint id " + selectedIcon.uid;
                }
            }
            selectedIcon = null;
        }
        #endregion

        #region PanelConnection
        private void cb_comboBoxConfigFile_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxConfigFile.SelectedIndex >= 0 && comboBoxConfigFile.SelectedIndex < comboBoxConfigFile.Items.Count)
            {
                configServerFileName = comboBoxConfigFile.SelectedItem as string;
                LoadServerConfigFile();
            }
        }
        private void cb_buttonAddConfigFile_Click(object sender, EventArgs e)
        {
            if (diagSelectCfgName.ShowDialog() == DialogResult.OK)
            {
                string cfgName = diagSelectCfgName.textBoxMsgToPlayer.Text;
                string filename = "cfgServer_" + cfgName + ".xml";
                FileInfo fi = new FileInfo(configPath + "\\" + filename);

                if(fi.Exists)
                {
                    MessageBox.Show("This config name already exists, please choose a new one");
                }
                else
                {
                    configServerFileName = cfgName;

                    comboBoxConfigFile.Items.Add(cfgName);
                    comboBoxConfigFile.SelectedItem = comboBoxConfigFile.Items[comboBoxConfigFile.Items.Count - 1];

                    // will fail to load and set default parameters
                    LoadServerConfigFile();
                }
            }
        }
        private void cb_buttonConnect_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;

            BattlEyeConnectionResult resultBE = BattlEyeConnectionResult.Success;

            myDatabase.LoginData login = new myDatabase.LoginData();
                login.Server = textBoxDBURL.Text;
                login.Port = int.Parse(numericUpDownDBPort.Text);
                login.DBname = textBoxDBBaseName.Text;
                login.Username = textBoxDBUser.Text;
                login.Password = textBoxDBPassword.Text;
                login.InstanceId = int.Parse(numericUpDownInstanceId.Text);
                login.Cfg = mySrvCfg;
            myDB.Connect(login);

            if ((int.Parse(numericUpDownrConPort.Text) != 0) && (textBoxrConPassword.Text != ""))
            {
                IPAddress ip = null;
                IPHostEntry hostEntry;

                try
                {
                    if (textBoxrConURL.Text != "")
                    {
                        hostEntry = Dns.GetHostEntry(textBoxrConURL.Text);
                    }
                    else
                    {
                        hostEntry = Dns.GetHostEntry(textBoxDBURL.Text);
                    }

                    foreach (IPAddress iph in hostEntry.AddressList)
                        if (iph.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            ip = iph;

                    int port = int.Parse(numericUpDownrConPort.Text);
                    string pass = textBoxrConPassword.Text;
                    rCon = new BattlEyeClient(new BattlEyeLoginCredentials(ip, port, pass));
                    rCon.BattlEyeMessageReceived += BattlEyeMessageReceived;
                    rCon.BattlEyeConnected += BattlEyeConnected;
                    rCon.BattlEyeDisconnected += BattlEyeDisconnected;
                    rCon.ReconnectDelay = 10;   // 10s delay before trying to reconnect

                    resultBE = rCon.Connect();
                }
                catch
                {
                    resultBE = BattlEyeConnectionResult.ConnectionFailed;
                }

                switch (resultBE)
                {
                    case BattlEyeConnectionResult.ConnectionFailed:
                    case BattlEyeConnectionResult.InvalidLogin:
                        rCon = null;
                        break;
                }
            }

            bool bDBconnected = myDB.Connected;
            bool bBEconnected = (rCon != null);

            led_database = (bDBconnected) ? LedStatus.On : LedStatus.Off;
            led_battleye = (bBEconnected) ? LedStatus.On : LedStatus.Off;

            if (!bDBconnected || !bBEconnected)
            {
                string header = (!bDBconnected) ? "Database " : "";
                header += (!bBEconnected) ? "BattlEye" : "";
                string db_error = (!bDBconnected) ? "Unable to connect to Database.\n" : "";
                string be_error = (!bBEconnected) ? (resultBE == BattlEyeConnectionResult.InvalidLogin) ? "Invalid rCon login" : "Unable to connect to BattlEye.\n" : "";
                MessageBox.Show(db_error + be_error, header);
            }

            if (myDB.Connected && resultBE != BattlEyeConnectionResult.ConnectionFailed)
            {
                mySrvCfg.instance_id = myDB.Schema.InstanceId.ToString();

                List<int> instances = myDB.GetInstanceList();
                bool bInstanceFound = myDB.Schema.InstanceId == instances.Find(x => (x == myDB.Schema.InstanceId));

                comboSelectInstance.Enabled = true;
                comboSelectInstance.Items.Clear();
                foreach (int i in instances)
                    comboSelectInstance.Items.Add(i);

                if (bInstanceFound == false)
                {
                    tabControl1.SelectedTab = tabPageSetup;
                    MessageBox.Show("Instance Id " + myDB.Schema.InstanceId + " not found in DB, please select one from the Setup tab");
                    comboSelectInstance.SelectedIndex = 0;
                }
                else
                {
                    for (int i = 0; i < comboSelectInstance.Items.Count; i++)
                    {
                        if ((int)comboSelectInstance.Items[i] == myDB.Schema.InstanceId)
                        {
                            comboSelectInstance.SelectedIndex = i;
                            break;
                        }
                    }
                }

                Enable(true);

                this.textBoxCmdStatus.Text = "";

                //
                string fullPath = configPath + "\\World" + mySrvCfg.BitmapName;
                if (Directory.Exists(fullPath))
                {
                    LoadBitmapConfigFile();
                }
                else
                {
                    MessageBox.Show("Please select a bitmap for your world, and don't forget to adjust the map to your bitmap with the map helper...", "No bitmap selected");

                    SelectBitmap();
                }

                SetCurrentMap();

                panelCnx.Enabled = false;
                panelCnx.Visible = false;
            }

            this.Cursor = Cursors.Arrow;
        }
        #endregion

        #region PanelDisplay
        private void cb_trackBarLastUpdated_ValueChanged(object sender, EventArgs e)
        {
            var track = sender as TrackBar;

            labelLastUpdate.Text = (track.Value == track.Maximum) ? "-" : track.Value.ToString();

            mySrvCfg.filter_last_updated = (track.Value == track.Maximum) ? 999 : track.Value;
            myDB.Schema.FilterLastUpdated = mySrvCfg.filter_last_updated;

            RefreshDB();
        }
        private void cb_trackBarMagLevel_ValueChanged(object sender, EventArgs e)
        {
            var track = sender as TrackBar;

            labelMagLevel.Text = (1 << track.Value).ToString();

            mySrvCfg.bitmap_mag_level = track.Value;
            virtualMap.nfo.mag_depth = virtualMap.nfo.max_depth + track.Value;
        }
        #endregion

        #region PanelScripts
        private void cb_buttonBackup_Click(object sender, EventArgs e)
        {
            string s_date = DateTime.Now.Year + "-"
                          + DateTime.Now.Month.ToString("00") + "-"
                          + DateTime.Now.Day.ToString("00") + " "
                          + DateTime.Now.Hour.ToString("00") + "h"
                          + DateTime.Now.Minute.ToString("00");
            saveFileDialog1.FileName = "Backup " + textBoxDBBaseName.Text + " " + s_date + ".sql";
            saveFileDialog1.CheckFileExists = false;
            saveFileDialog1.CheckPathExists = true;
            saveFileDialog1.Filter = "SQL file|*.sql";
            saveFileDialog1.DefaultExt = "sql";
            saveFileDialog1.Title = "Save SQL file...";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                this.Cursor = Cursors.WaitCursor;

                string result = myDB.BackupToFile(saveFileDialog1.FileName);

                textBoxCmdStatus.Text = result;

                this.Cursor = Cursors.Arrow;
            }
        }
        private void cb_buttonRemoveDestroyed_Click(object sender, EventArgs e)
        {
            int res = myDB.ExecuteSqlNonQuery("DELETE FROM instance_vehicle WHERE instance_id=" + mySrvCfg.instance_id + " AND damage=1");
            if (res > 0)
            {
                RefreshDB();
            }
            textBoxCmdStatus.Text = "removed " + res + " destroyed vehicles.";
        }
        private void cb_buttonSpawnNew_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;

            int result = myDB.SpawnNewVehicles(int.Parse(textBoxVehicleMax.Text));
            if (result > 0)
            {
                RefreshDB();
            }
            textBoxCmdStatus.Text = "spawned " + result + " new vehicles.";

            this.Cursor = Cursors.Arrow;
        }
        private void cb_buttonRemoveBodies_Click(object sender, EventArgs e)
        {
            int limit = int.Parse(textBoxOldBodyLimit.Text);

            int res = myDB.RemoveBodies(limit);
            if (res > 0)
            {
                RefreshDB();
            }
            textBoxCmdStatus.Text = "removed " + res + " bodies older than " + limit + " days.";
        }
        private void cb_buttonRemoveTents_Click(object sender, EventArgs e)
        {
            int limit = int.Parse(textBoxOldTentLimit.Text);

            string query = "DELETE FROM id using instance_deployable id inner join deployable d on id.deployable_id = d.id"
                         + " inner join survivor s on id.owner_id = s.id and s.is_dead=1"
                         + " WHERE id.instance_id=" + mySrvCfg.instance_id + " AND d.class_name = 'TentStorage' AND id.last_updated < now() - interval " + limit + " day";
            int res = myDB.ExecuteSqlNonQuery(query);
            if (res > 0)
            {
                RefreshDB();
            }
        }
        private void cb_buttonSelectCustom_Click(object sender, EventArgs e)
        {
            Button btSel = sender as Button;

            openFileDialog1.FileName = "";
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;
            openFileDialog1.Multiselect = false;
            openFileDialog1.Filter = "SQL & BAT files|*.sql;*.bat|SQL files|*.sql|Batch files|*.bat";

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Button btExe = null;
                switch (btSel.Name)
                {
                    case "buttonSelectCustom1": btExe = this.buttonCustom1; myComCfg.customscript1 = openFileDialog1.FileName; break;
                    case "buttonSelectCustom2": btExe = this.buttonCustom2; myComCfg.customscript2 = openFileDialog1.FileName; break;
                    case "buttonSelectCustom3": btExe = this.buttonCustom3; myComCfg.customscript3 = openFileDialog1.FileName; break;
                }
                if (btExe != null)
                {
                    FileInfo fi = new FileInfo(openFileDialog1.FileName);
                    btExe.Text = fi.Name;
                    btExe.Enabled = true;
                }
            }
        }
        private void cb_buttonCustom_Click(object sender, EventArgs e)
        {
            Button bt = sender as Button;

            string fullpath = null;
            switch (bt.Name)
            {
                case "buttonCustom1": fullpath = myComCfg.customscript1; break;
                case "buttonCustom2": fullpath = myComCfg.customscript2; break;
                case "buttonCustom3": fullpath = myComCfg.customscript3; break;
            }

            if (Tool.NullOrEmpty(fullpath))
                return;

            this.Cursor = Cursors.WaitCursor;
            try
            {
                FileInfo fi = new FileInfo(fullpath);

                switch (fi.Extension.ToLowerInvariant())
                {
                    case ".sql":
                        StreamReader sr = fi.OpenText();
                        string queries = sr.ReadToEnd();

                        string result = myDB.ExecuteScript(queries);

                        this.textBoxCmdStatus.Text += result;

                        sr.Close();
                        break;

                    case ".bat":
                        System.Diagnostics.Process proc = System.Diagnostics.Process.Start(fullpath);
                        proc.WaitForExit();
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception found");
            }
            this.Cursor = Cursors.Arrow;
        }
        #endregion
        
        #region PanelVehicles
        private void cb_toolStripMenuItemResetTypes_Click(object sender, EventArgs e)
        {
            var item = sender as ToolStripMenuItem;
            var menu = item.Owner as ContextMenuStrip;

            switch (menu.SourceControl.Name)
            {
                case "dataGridViewVehicleTypes":
                    {
                        // Remove every unused types
                        List<DataRow> toRemove = new List<DataRow>();
                        foreach (DataRow row in mySrvCfg.vehicle_types.Tables[0].Rows)
                        {
                            if (myDB.Vehicles.Tables[0].Rows.FindFrom("class_name", row.Field<string>("ClassName")) == null)
                                toRemove.Add(row);
                        }

                        foreach (DataRow row in toRemove)
                            mySrvCfg.vehicle_types.Tables[0].Rows.Remove(row);
                    }
                    break;
                case "dataGridViewDeployableTypes":
                    {
                        // Remove every unused types
                        List<DataRow> toRemove = new List<DataRow>();
                        foreach (DataRow row in mySrvCfg.deployable_types.Tables[0].Rows)
                        {
                            if (myDB.Deployables.Tables[0].Rows.FindFrom("class_name", row.Field<string>("ClassName")) == null)
                                toRemove.Add(row);
                        }

                        foreach (DataRow row in toRemove)
                            mySrvCfg.deployable_types.Tables[0].Rows.Remove(row);
                    }
                    break;
            }

            // ??? myDB.OnConnection();
        }
        private void cb_dataGridViewVehicleTypes_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if ((e.RowIndex < 0) || (e.RowIndex >= dataGridViewVehicleTypes.Rows.Count))
                return;

            // Ignore clicks that are not on checkbox cells.  
            if (e.ColumnIndex == dataGridViewVehicleTypes.Columns["ColGVVTShow"].Index)
            {
                dataGridViewVehicleTypes.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }
        private void cb_dataGridViewVehicleTypes_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if ((e.RowIndex < 0) || (e.RowIndex >= dataGridViewVehicleTypes.Rows.Count))
                return;

            bool bState = (bool)dataGridViewVehicleTypes["ColGVVTShow", e.RowIndex].Value;

            DataRow row = mySrvCfg.vehicle_types.Tables[0].Rows.Find(dataGridViewVehicleTypes["ColGVVTClassName", e.RowIndex].Value);

            row.SetField<bool>("Show", bState);
        }
        private static bool GVVT_bCurrentState = true;
        private void cb_dataGridViewVehicleTypes_ColumnHeaderMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex == dataGridViewVehicleTypes.Columns["ColGVVTShow"].Index)
            {
                GVVT_bCurrentState = !GVVT_bCurrentState;

                foreach (DataRow row in mySrvCfg.vehicle_types.Tables[0].Rows)
                    row.SetField<bool>("Show", GVVT_bCurrentState);
            }
        }
        #endregion
        
        #region PanelDeployables
        private void cb_dataGridViewDeployableTypes_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if ((e.RowIndex < 0) || (e.RowIndex >= dataGridViewDeployableTypes.Rows.Count))
                return;

            // Ignore clicks that are not on checkbox cells.  
            if (e.ColumnIndex == dataGridViewDeployableTypes.Columns["ColGVDTShow"].Index)
            {
                dataGridViewDeployableTypes.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }
        private void cb_dataGridViewDeployableTypes_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if ((e.RowIndex < 0) || (e.RowIndex >= dataGridViewDeployableTypes.Rows.Count))
                return;

            bool bState = (bool)dataGridViewDeployableTypes["ColGVDTShow", e.RowIndex].Value;

            DataRow row = mySrvCfg.deployable_types.Tables[0].Rows.Find(dataGridViewDeployableTypes["ColGVDTClassName", e.RowIndex].Value);

            row.SetField<bool>("Show", bState);
        }
        private static bool GVDT_bCurrentState = true;
        private void cb_dataGridViewDeployableTypes_ColumnHeaderMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex == dataGridViewDeployableTypes.Columns["ColGVDTShow"].Index)
            {
                GVDT_bCurrentState = !GVDT_bCurrentState;

                foreach (DataRow row in mySrvCfg.deployable_types.Tables[0].Rows)
                    row.SetField<bool>("Show", GVDT_bCurrentState);
            }
        }
        #endregion
        
        #region PanelOnline
        private void messageToPlayerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if ((rCon == null) || !rCon.Connected)
                return;

            var menu = (sender as ToolStripMenuItem).Owner as ContextMenuStrip;
            if ((menu.SourceControl.Name == "dataGridViewPlayers") && (dataGridViewPlayers.CurrentRow != null))
            {
                diagMsgToPlayer.Text = "Send message to " + dataGridViewPlayers.CurrentRow.Cells["Name"].Value as string;

                if (diagMsgToPlayer.ShowDialog() == DialogResult.OK)
                {
                    int id = (int)dataGridViewPlayers.CurrentRow.Cells["Id"].Value;

                    rCon.SendCommand(BattlEyeCommand.Say, id + " [" + mySrvCfg.rcon_adminname + "] : " + diagMsgToPlayer.textBoxMsgToPlayer.Text);
                }
                diagMsgToPlayer.textBoxMsgToPlayer.Text = "";
            }
        }
        private void kickPlayerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if ((rCon == null) || !rCon.Connected)
                return;

            var menu = (sender as ToolStripMenuItem).Owner as ContextMenuStrip;
            if ((menu.SourceControl.Name == "dataGridViewPlayers") && (dataGridViewPlayers.CurrentRow != null))
            {
                diagMsgToPlayer.Text = "Kick " + dataGridViewPlayers.CurrentRow.Cells["Name"].Value as string;

                if (diagMsgToPlayer.ShowDialog() == DialogResult.OK)
                {
                    int id = (int)dataGridViewPlayers.CurrentRow.Cells["Id"].Value;
                    rCon.SendCommand(BattlEyeCommand.Kick, id + " " + diagMsgToPlayer.textBoxMsgToPlayer.Text);
                }
                diagMsgToPlayer.textBoxMsgToPlayer.Text = "";
            }
        }
        #endregion
        
        #region PanelSetup
        private void cb_numericDBRefreshRate_ValueChanged(object sender, EventArgs e)
        {
            var numeric = sender as NumericUpDown;

            myComCfg.db_refreshrate = numeric.Value;
        }
        private void cb_numericBERefreshRate_ValueChanged(object sender, EventArgs e)
        {
            var numeric = sender as NumericUpDown;

            myComCfg.be_refreshrate = numeric.Value;
        }
        private void cb_comboSelectInstance_SelectedValueChanged(object sender, EventArgs e)
        {
            ComboBox cb = sender as ComboBox;

            myDB.Schema.InstanceId = (int)cb.SelectedItem;
            mySrvCfg.instance_id = myDB.Schema.InstanceId.ToString();
            numericUpDownInstanceId.Value = myDB.Schema.InstanceId;
        }
        private void cb_comboSelectEpochWorld_SelectedValueChanged(object sender, EventArgs e)
        {
            ComboBox cb = sender as ComboBox;

            foreach (KeyValuePair<string, List<Tool.Point[]>> pair in Tool.mapHelperDefs)
            {
                if(pair.Key == cb.SelectedItem as string)
                {
                    mySrvCfg.world_id = 0/*pair.Key*/;
                    mySrvCfg.world_name = pair.Key;

                    mapHelper = new MapHelper(virtualMap, mySrvCfg.world_name);
                    break;
                }
            }
        }
        private void cb_cbCartographer_CheckedChanged(object sender, EventArgs e)
        {
            if ((sender as CheckBox).Checked == true)
                cartographer.paths.Clear();

            bCartographer ^= true;
        }
        #endregion

        #region ToolStrip
        private void SelectBitmap()
        {
            openFileDialog1.FileName = "";
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;
            openFileDialog1.Multiselect = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    FileInfo fi = new FileInfo(openFileDialog1.FileName);
                    if (fi.Exists)
                    {
                        DirectoryInfo di = new DirectoryInfo(configPath + "\\World" + fi.Name);
                        string tileBasePath = di.FullName + "\\LOD";

                        if (di.Exists)
                        {
                            DialogResult res = MessageBox.Show("Tiles already exist, replace & regenerate from bitmap ? or keep existing tiles...", "Load bitmap", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

                            if (res == DialogResult.Yes)
                            {
                                di.Delete(true);

                                GenerateTilesForBitmap(fi, tileBasePath);
                            }
                            else
                            {
                                mySrvCfg.BitmapName = fi.Name;
                            }
                        }
                        else
                        {
                            GenerateTilesForBitmap(fi, tileBasePath);
                        }

                        LoadBitmapConfigFile();

                        // reset tile cache
                        mtxTileUpdate.WaitOne();
                        dicTileExistence = new Dictionary<int, bool>();
                        mtxTileUpdate.ReleaseMutex();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error while generating tiles !\r\nMaybe the bitmap is too large to be processed, max size is 16384*16384...");
                    textBoxCmdStatus.Text += ex.ToString();
                    this.Cursor = Cursors.Arrow;
                }
            }
        }

        private void GenerateTilesForBitmap(FileInfo fi, string tileBasePath)
        {
            MessageBox.Show("Please wait while generating tiles...\r\nThis is done once when selecting a new map.");

            mtxTileUpdate.WaitOne();
            tileCache.Clear();
            mtxTileUpdate.ReleaseMutex();

            this.Cursor = Cursors.WaitCursor;

            Tuple<Tool.Size, Tool.Size, Tool.Size> sizes = Tool.CreateTiles(fi.FullName, tileBasePath, 256);

            myBmpCfg.RatioX = sizes.Item1.Width / sizes.Item2.Width;
            myBmpCfg.RatioY = sizes.Item1.Height / sizes.Item2.Height;
            myBmpCfg.TileSizeX = (int)sizes.Item3.Width;
            myBmpCfg.TileSizeY = (int)sizes.Item3.Height;
            int tileCount = (int)(sizes.Item2.Width / 256);
            myBmpCfg.TileDepth = (int)Math.Log(tileCount, 2) + 1;

            mySrvCfg.BitmapName = fi.Name;

            SaveBitmapConfigFile();

            this.Cursor = Cursors.Arrow;
            MessageBox.Show("Tiles generation done.");
        }
        
        private void cb_textBoxChatInput_TextChanged(object sender, EventArgs e)
        {
            if (textBoxChatInput.Text.EndsWith("\n"))
            {
                if ((rCon != null) && rCon.Connected)
                {
                    rCon.SendCommand(BattlEyeCommand.Say, "-1 [" + mySrvCfg.rcon_adminname + "] : " + textBoxChatInput.Text.TrimEnd('\n'));
                }
                textBoxChatInput.Text = "";
            }
        }
        private void cb_richTextBoxChat_TextChanged(object sender, EventArgs e)
        {
            richTextBoxChat.SelectionStart = richTextBoxChat.TextLength;
            richTextBoxChat.SelectionLength = 0;
            richTextBoxChat.Focus();
            textBoxChatInput.Focus();
        }
        private void AddTextInChat(string text, Color color)
        {
            string time = DateTime.Now.ToString();
            richTextBoxChat.AppendText( "\n["+time + "] " + text, color);
        }
        #endregion

        #region BattlEye
        private void BattlEyeConnected(BattlEyeConnectEventArgs args)
        {
            bool bSuccess = (args.ConnectionResult == BattlEyeConnectionResult.Success);

            richTextBoxChat.Invoke((System.Threading.ThreadStart)(delegate
            {
                BattlEyeConnectionMessage(args.Message, bSuccess);
            }));
        }
        private void BattlEyeDisconnected(BattlEyeDisconnectEventArgs args)
        {
            richTextBoxChat.Invoke((System.Threading.ThreadStart)(delegate
            {
                BattlEyeConnectionMessage(args.Message, false);
            }));
        }
        private void BattlEyeConnectionMessage(string msg, bool connected)
        {
            AddTextInChat(msg, connected ? Color.Green : Color.Red);

            led_battleye = connected ? LedStatus.On : LedStatus.Off;

            if (connected == false)
            {
                PlayerNamesOnline.Tables[0].Rows.Clear();
                PlayersOnline.Tables[0].Rows.Clear();
                AdminsOnline.Tables[0].Rows.Clear();
            }
        }
        public class PlayerData
        {
            public int Id { get; set; }
            public string Ip { get; set; }
            public string Guid { get; set; }
            public string Name { get; set; }
            public string Status { get; set; }
            public bool Processed = false;
        }
        public class AdminData
        {
            public int Id { get; set; }
            public string Ip { get; set; }
            public int Port { get; set; }
            public bool Processed = false;
        }
        private List<PlayerData> players = new List<PlayerData>();
        private List<AdminData> admins = new List<AdminData>();
        private void BattlEyeMessageReceived(BattlEyeMessageEventArgs args)
        {
            try
            {
                if (args.Message.StartsWith("Players on server"))
                {
                    // Player list
                    //  format [#] [IP Address]:[Port] [Ping] [GUID] [Name]
                    StringReader sr = new StringReader(args.Message);

                    string line;
                    do
                    {
                        line = sr.ReadLine();
                    } while (line.EndsWith("----") == false);

                    line = sr.ReadLine();

                    // http://www.txt2re.com/
                    string re = "(\\d+)\\s+((?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?))(?![\\d]):(\\d+)\\s+(\\d+|\\-1)\\s+.*?((?:[a-z0-9]*)).*?((?:[a-z]+|\\?)).*?\\s+((?:.*))";

                    Regex r = new Regex(re, RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    while (line != null && line.StartsWith("(") == false)
                    {
                        Match m = r.Match(line);
                        if (m.Success)
                        {
                            PlayerData entry = new PlayerData();
                            entry.Id = int.Parse(m.Groups[1].ToString());
                            entry.Name = m.Groups[7].ToString();
                            entry.Guid = m.Groups[5].ToString();
                            entry.Ip = m.Groups[2].ToString();
                            if(entry.Name.Contains("(Lobby)"))
                            {
                                entry.Name = entry.Name.Replace("(Lobby)", "").Trim();
                                entry.Status = "Lobby";
                            }
                            else
                            {
                                entry.Status = "Ingame";
                            }

                            players.Add(entry);
                        }

                        line = sr.ReadLine();
                    }

                    this.Invoke((System.Threading.ThreadStart)(delegate { UpdatePlayersOnline(); }));
                }
                else if (args.Message.StartsWith("Connected RCon admins"))
                {
                    // Admin list
                    //  format [#] [IP Address]:[Port]\n
                    StringReader sr = new StringReader(args.Message);

                    string line;
                    do
                    {
                        line = sr.ReadLine();
                    } while (line.EndsWith("----") == false);

                    line = sr.ReadLine();

                    // http://www.txt2re.com/
                    string re1 = "(\\d+)";	// Id
                    string re2 = "(\\s+)";
                    string re3 = "((?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?))(?![\\d])";	// IPv4 IP Address 1
                    string re4 = "(:)";
                    string re5 = "(\\d+)";	// Port number
                    Regex r = new Regex(re1 + re2 + re3 + re4 + re5, RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    while (line != null && line.StartsWith("(") == false)
                    {
                        Match m = r.Match(line);
                        if (m.Success)
                        {
                            AdminData entry = new AdminData();
                            entry.Id = int.Parse(m.Groups[1].ToString());
                            entry.Ip = m.Groups[3].ToString();
                            entry.Port = int.Parse(m.Groups[5].ToString());

                            admins.Add(entry);
                        }

                        line = sr.ReadLine();
                    }

                    this.Invoke((System.Threading.ThreadStart)(delegate { UpdateAdminsOnline(); }));
                }
                else
                {
                    richTextBoxChat.Invoke((System.Threading.ThreadStart)(delegate
                    {
                        Color col = (args.Message.StartsWith("RCon admin #")) ? Color.DarkOrange : Color.Black;
                        AddTextInChat(args.Message, col);
                    }));
                }
            }
            catch
            {
                richTextBoxChat.Invoke((System.Threading.ThreadStart)(delegate { AddTextInChat("Error retrieving players.", Color.Red); }));
            }
        }
        private void UpdatePlayersOnline()
        {
            PlayerNamesOnline.Tables[0].Rows.Clear();
            foreach(PlayerData player in players)
            {
                PlayerNamesOnline.Tables[0].Rows.Add(player.Name, player.Status);
            }

            List<DataRow> toRemove = new List<DataRow>();
            foreach (DataRow row in PlayersOnline.Tables[0].Rows)
            {
                PlayerData found = players.Find(
                    delegate(PlayerData data)
                    {
                        return (data.Id == row.Field<int>("Id"));
                    });
                if (found != null)
                {
                    if (row.Field<string>("Name") != found.Name)
                        row.SetField<string>("Name", found.Name);

                    if (row.Field<string>("GUID") != found.Guid)
                        row.SetField<string>("GUID", found.Guid);

                    if (row.Field<string>("IP") != found.Ip)
                        row.SetField<string>("IP", found.Ip);

                    if (row.Field<string>("Status") != found.Status)
                        row.SetField<string>("Status", found.Status);

                    found.Processed = true;
                }
                else
                {
                    toRemove.Add(row);
                }
            }
            foreach (DataRow row in toRemove)
                PlayersOnline.Tables[0].Rows.Remove(row);

            foreach (PlayerData player in players)
            {
                if (player.Processed == false)
                {
                    PlayersOnline.Tables[0].Rows.Add(player.Id, player.Name, player.Guid, player.Ip, player.Status);
                }
            }
            players.Clear();
        }
        private void UpdateAdminsOnline()
        {
            List<DataRow> toRemove = new List<DataRow>();
            foreach (DataRow row in AdminsOnline.Tables[0].Rows)
            {
                AdminData found = admins.Find(
                    delegate(AdminData data)
                    {
                        return (data.Id == row.Field<int>("Id"));
                    });
                if (found != null)
                {
                    string ip = LocalResolveIP(found.Ip);

                    if (row.Field<string>("IP") != ip)
                        row.SetField<string>("IP", ip);

                    if (row.Field<int>("Port") != found.Port)
                        row.SetField<int>("Port", found.Port);

                    found.Processed = true;
                }
                else
                {
                    toRemove.Add(row);
                }
            }
            foreach (DataRow row in toRemove)
                AdminsOnline.Tables[0].Rows.Remove(row);

            foreach (AdminData admin in admins)
            {
                if (admin.Processed == false)
                {
                    admin.Ip = LocalResolveIP(admin.Ip);

                    AdminsOnline.Tables[0].Rows.Add(admin.Id, admin.Ip, admin.Port);
                }
            }
            admins.Clear();
        }
        #endregion

        #region BackgroundWorkers
        private enum LedStatus
        {
            Off,
            On,
            Active
        }
        LedStatus led_database = LedStatus.Off;
        LedStatus led_databaseInternal = LedStatus.Off;

        LedStatus led_battleye = LedStatus.Off;
        LedStatus led_battleyeInternal = LedStatus.Off;

        Bitmap LedStatusToBitmap(LedStatus status)
        {
            switch (status)
            {
                case LedStatus.On: return global::DBAccess.Properties.Resources.Tool_LedOn;
                case LedStatus.Active: return global::DBAccess.Properties.Resources.Tool_LedAccess;
            }

            return global::DBAccess.Properties.Resources.Tool_LedOff;
        }
        private void cb_bgWorkerRefreshLeds_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            while (!bw.CancellationPending)
            {
                Thread.Sleep(125);

                if(led_databaseInternal != led_database)
                    this.Invoke((System.Threading.ThreadStart)(delegate
                    {
                        led_databaseInternal = led_database;
                        toolStripStatusLedDB.Image = LedStatusToBitmap(led_databaseInternal);
                    }));

                if (led_battleyeInternal != led_battleye)
                    this.Invoke((System.Threading.ThreadStart)(delegate
                    {
                        led_battleyeInternal = led_battleye;
                        toolStripStatusLedBE.Image = LedStatusToBitmap(led_battleyeInternal);
                    }));
            }
        }
        private void cb_bgWorkerRefreshDatabase_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            // Initialize remaining time to 5sec for 1st DB refresh
            long remaining_ticks = 5 * 10000000;

            while (!bw.CancellationPending)
            {
                long last_ticks = DateTime.Now.Ticks;
                Thread.Sleep(125);
                remaining_ticks -= (DateTime.Now.Ticks - last_ticks);

                if (remaining_ticks <= 0)
                {
                    remaining_ticks = (long)(10000000 * myComCfg.db_refreshrate);

                    RefreshDB();
                }
            }
        }

        private void RefreshDB()
        {
            led_database = LedStatus.Active;

            myDB.Refresh();

            if (System.Threading.Interlocked.CompareExchange(ref bUserAction, 1, 0) == 0)
            {
                dlgUpdateIcons = this.BuildIcons;

                if (myDB.Connected)
                    this.Invoke(dlgUpdateIcons);

                System.Threading.Interlocked.Exchange(ref bUserAction, 0);
            }

            led_database = (myDB.Connected) ? LedStatus.On : LedStatus.Off;
        }
        private void cb_bgWorkerRefreshBattEye_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            // Initialize remaining time to 5sec for 1st refresh
            long remaining_ticks = 50000000;

            while (!bw.CancellationPending)
            {
                long last_ticks = DateTime.Now.Ticks;
                Thread.Sleep(125);
                remaining_ticks -= (DateTime.Now.Ticks - last_ticks);

                if (remaining_ticks <= 0)
                {
                    remaining_ticks = (long)(10000000 * myComCfg.be_refreshrate);

                    if ((rCon != null) && rCon.Connected)
                    {
                        led_battleye = LedStatus.Active;

                        rCon.SendCommand(BattlEyeCommand.Players);
                        rCon.SendCommand("Admins");

                        Thread.Sleep(125);
                        led_battleye = LedStatus.On;
                    }
                    else
                    {
                        led_battleye = LedStatus.Off;
                    }
                }
            }
        }
        private void cb_bgWorkerMapZoom_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            while (!bw.CancellationPending)
            {
                eventMapZoomBgWorker.WaitOne();

                if (virtualMap.Enabled)
                {
                    if (System.Threading.Interlocked.CompareExchange(ref bUserAction, 1, 0) == 0)
                    {
                        while (mapZoom.Update(virtualMap))
                        {
                            dlgRefreshMap = this.ApplyMapChanges;
                            this.Invoke(dlgRefreshMap);

                            Thread.Sleep(10);
                        }

                        dlgRefreshMap = this.ApplyMapChanges;
                        this.Invoke(dlgRefreshMap);
                    }
                    System.Threading.Interlocked.CompareExchange(ref bUserAction, 0, 1);
                }
            }
        }
        private void cb_bgWorkerFocus_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            while (!bw.CancellationPending)
            {
                Thread.Sleep(250);

                if (myDB.Connected)
                {
                    DelegateVoid dlg = FocusOnControlAtCursor;
                    this.Invoke(dlg);
                }
            }
        }
        private Control lastFocusCtrl = null;
        private void FocusOnControlAtCursor()
        {
            Control overCtrl = Tool.FindControlAtCursor(this);
            if (overCtrl != null && overCtrl != lastFocusCtrl)
            {
                if (overCtrl is SplitterPanel || overCtrl is TrackBar || overCtrl is DataGridView)
                {
                    overCtrl.Focus();
                    lastFocusCtrl = overCtrl;
                }
            }
        }
        private void cb_bgWorkerLoadTiles_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            while (!bw.CancellationPending)
            {
                bool bCacheChanged = false;

                List<tileReq> toLoad = new List<tileReq>();

                //
                //  fast: Check cache validity (mutexed)
                //
                mtxTileRequest.WaitOne();
                {
                    tileReq[] toCheck = tileRequests.ToArray();

                    long now_ticks = DateTime.Now.Ticks;

                    foreach (tileReq req in toCheck)
                    {
                        if (TileFileExists(req))
                        {
                            tileNfo nfo = tileCache.Find(x => req.path == x.path);
                            if (nfo == null)
                                toLoad.Add(req);
                            else
                                nfo.ticks = now_ticks;
                        }
                    }

                    bCacheChanged = (toLoad.Count != 0);
                }
                mtxTileRequest.ReleaseMutex();

                //
                //  heavy: Loading (not mutexed)
                //
                //foreach (tileReq req in toLoad)
                if(toLoad.Count > 0)
                {
                    tileReq req = toLoad[0];
                    tileNfo nfo = new tileNfo(req);

                    // each tile loaded is immediately inserted in cache
                    mtxTileUpdate.WaitOne();
                    tileCache.Add(nfo);
                    dicTileExistence[req.Key] = true;
                    mtxTileUpdate.ReleaseMutex();
                }

                //
                //  fast: Update cache (mutexed)
                //
                mtxTileUpdate.WaitOne();
                {
                    long now_ticks = DateTime.Now.Ticks;

                    int cacheSizeBefore = tileCache.Count;
                    tileCache.RemoveAll(x => (now_ticks - x.ticks > x.timeOut) && !x.bKeepLoaded);
                }
                mtxTileUpdate.ReleaseMutex();

                if (bCacheChanged)
                    this.Invoke((System.Threading.ThreadStart)(delegate { splitContainer1.Panel1.Invalidate(); }));

                Thread.Sleep(5);
            }
        }
        #endregion
    }
}