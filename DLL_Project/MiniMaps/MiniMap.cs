﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using UnityEngine;
using Verse;

namespace CommunityCoreLibrary.MiniMap
{
    
    public class MiniMap
    {
        
        #region Instance Data

        private bool                    _hidden = false;

        public MiniMapDef               miniMapDef;

        public List<MiniMapOverlay>     overlayWorkers;

        private Texture2D               _iconTexture;

        public bool                     dirty;

        #endregion Instance Data

        #region Constructors

        public                          MiniMap( MiniMapDef miniMapDef )
        {
            this.miniMapDef = miniMapDef;
            this._hidden = this.miniMapDef.hiddenByDefault;

            overlayWorkers = new List<MiniMapOverlay>();

            for( int index = 0; index < this.miniMapDef.overlays.Count; ++index )
            {
                var overlayData = this.miniMapDef.overlays[ index ];
                if(
                    ( overlayData.overlayClass == null )||
                    (
                        ( overlayData.overlayClass != typeof(MiniMapOverlay ) )&&
                        ( !overlayData.overlayClass.IsSubclassOf( typeof(MiniMapOverlay ) ) )
                    )
                )
                {
                    CCL_Log.Trace(
                        Verbosity.NonFatalErrors,
                        string.Format( "Unable to resolve overlayClass for '{0}' at index {1} to 'CommunityCoreLibrary.MiniMapOverlay'", miniMapDef.defName, index )
                    );
                    return;
                }
                else
                {
                    var overlayWorker = (MiniMapOverlay)Activator.CreateInstance( overlayData.overlayClass, new System.Object[] { this, overlayData } );
                    if( overlayWorker == null )
                    {
                        CCL_Log.Trace(
                            Verbosity.NonFatalErrors,
                            string.Format( "Unable to create instance of '{0}' for '{1}'", overlayData.overlayClass.Name, miniMapDef.defName )
                        );
                        return;
                    }
                    else
                    {
                        overlayWorkers.Add( overlayWorker );
                        CCL_Log.Trace(
                            Verbosity.Injections,
                            string.Format( "Added overlay '{0}' to '{1}' at draw position {2}", overlayData.overlayClass.Name, this.miniMapDef.defName, ( this.miniMapDef.drawOrder + overlayData.drawOffset ) )
                        );
                    }
                }
            }

            dirty = true;
        }

        #endregion Constructors

        #region Properties

        public List<MiniMapOverlay>     VisibleOverlays
        {
            get
            {
                return overlayWorkers.Where( worker => !worker.Hidden ).OrderBy( worker => worker.overlayDef.drawOffset ).ToList();
            }
        }

        public bool                     IsOrHasIConfigurable
        {
            get
            {
                if( this is IConfigurable )
                {
                    return true;
                }
                foreach( var overlay in overlayWorkers )
                {
                    if( overlay is IConfigurable )
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public virtual bool             Hidden
        {
            get
            {
                return _hidden;
            }
            set
            {
                _hidden = value;

                if( !_hidden )
                {
                    // Mark as dirty for immediate update
                    dirty = true;
                }

                // mark the controller dirty so overlays get re-ordered.
                MiniMapController.dirty = true;
            }
        }

        public virtual Texture2D        Icon
        {
            get
            {
                if(
                    ( _iconTexture == null )&&
                    ( !this.miniMapDef.iconTex.NullOrEmpty() )
                )
                {
                    _iconTexture = ContentFinder<Texture2D>.Get( this.miniMapDef.iconTex, true );
                }
                if( _iconTexture.NullOrBad() )
                {
                    return BaseContent.BadTex;
                }
                return _iconTexture;
            }
        }

        public virtual string           label
        {
            get
            {
                if( miniMapDef.labelKey.NullOrEmpty() )
                {
                    return miniMapDef.label;
                }
                return miniMapDef.labelKey.Translate();
            }
        }

        public virtual string           LabelCap
        {
            get
            {
                return label.CapitalizeFirst();
            }
        }

        public virtual string           ToolTip
        {
            get
            {
                // Get tool tip (w/ description)
                // Use core translations for "Off" and "On"
                var tipString = string.Empty;
                if( !miniMapDef.description.NullOrEmpty() )
                {
                    tipString = miniMapDef.description + "\n\n";
                }
                tipString += "MiniMap.OverlayIconTip".Translate( LabelCap, Hidden ? "Off".Translate() : "On".Translate() );
                tipString += "\n\n";
                if( overlayWorkers.Count > 1 )
                {
                    for( int index = 0; index < overlayWorkers.Count; ++index )
                    {
                        var worker = overlayWorkers[ index ];
                        tipString += "MiniMap.OverlayIconTip".Translate( worker.LabelCap, worker.Hidden ? "Off".Translate() : "On".Translate() );
                        tipString += "\n";
                    }
                    tipString += "\n";
                }
                tipString += "MiniMap.Toggle".Translate();
                return tipString;
            }
        }

        #endregion Properties

        #region Methods

        public void                     ClearTextures( bool apply = false )
        {
            foreach ( var overlay in overlayWorkers )
            {
                overlay.ClearTexture( apply );
                overlay.texture.Apply();
            }
        }

        public virtual void             Update()
        {
        }

        public virtual void             DrawOverlays( Rect inRect )
        {
            var workers = VisibleOverlays;
            if( workers.Any() )
            {
                foreach( var worker in workers )
                {
                    GUI.DrawTexture( inRect, worker.texture );
                }
            }
        }

        public virtual List<FloatMenuOption>  GetFloatMenuOptions()
        {
            List<FloatMenuOption> options = overlayWorkers.SelectMany( worker => worker.GetFloatMenuOptions() ).ToList();
            return options;
        }

        #endregion Methods
    }
}