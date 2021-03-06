﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Hotfix;
using Game.Knight;
using UnityEngine.UI;
using Core;
using UnityEngine;
using Framework.WindUI;

namespace KnightHotfixModule.Knight.GUI
{
    public class CreatePlayerItem : MonoBehaviourProxy
    {
        public Toggle                       SelectedPlayer;
        public CreatePlayerView             Parent;
        public int                          ProfessionalID;

        private Actor.ActorCreateRequest    mActorCreateRequest;

        public override void SetObjects(List<UnityObject> rObjs, List<BaseDataObject> rBaseDatas)
        {
            base.SetObjects(rObjs, rBaseDatas);
            this.SelectedPlayer = this.Objects[0].Object as Toggle;
            this.Parent = (this.Objects[0].Object as View).ViewController as CreatePlayerView;
        }

        public void OnToggleSelectedValueChanged()
        {
            if (this.SelectedPlayer.isOn && this.Parent.CurrentSelectedItem != this)
            {
                StartLoad();
                this.Parent.CurrentSelectedItem = this;
            }
            else if (!this.SelectedPlayer.isOn)
            {
                StopLoad();
            }
        }

        public void StartLoad()
        {
            ActorProfessional rProfessional = GameConfig.Instance.GetActorProfessional(this.ProfessionalID);
            this.Parent.ProfessionalDesc.text = rProfessional.Desc;
            mActorCreateRequest = Actor.CreateActor(-1, rProfessional.HeroID, ActorLoadCompleted);
        }

        public void StopLoad()
        {
            if (mActorCreateRequest != null)
            {
                UtilTool.SafeDestroy(mActorCreateRequest.actor.ExhibitActor.ActorGo);
                mActorCreateRequest.Stop();
            }
        }

        private void ActorLoadCompleted(Actor rActor)
        {
            var rActorPos = rActor.ActorGo.transform.position;
            RaycastHit rHitInfo;
            if (Physics.Raycast(rActorPos + Vector3.up * 5.0f, Vector3.down, out rHitInfo, 20, 1 << LayerMask.NameToLayer("Road")))
            {
                rActorPos = new Vector3(rActorPos.x, rHitInfo.point.y, rActorPos.z);
            }
            rActor.ActorGo.transform.position = rActorPos;
        }
    }
}
