﻿//======================================================================
//        Copyright (C) 2015-2020 Winddy He. All rights reserved
//        Email: hgplan@126.com
//======================================================================
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Core;

namespace Framework.WindUI
{
    /// <summary>
    /// UI的管理类
    /// </summary>
    public class UIManager : MonoBehaviour 
    {
        private static UIManager    __instance;
        public  static UIManager    Instance { get { return __instance; } }
    
        /// <summary>
        /// 存放各种动态节点的地方
        /// </summary>
        public GameObject           rootCanvas;
    
        /// <summary>
        /// 当前的UI中的Views，每个View是用GUID来作唯一标识
        /// 底层-->顶层 { 0 --> list.count }
        /// </summary>
        private Dict<string, View>  curViews;
        
        /// <summary>
        /// 当前存在的固定View，每个View使用GUID来作唯一标识
        /// </summary>
        private Dict<string, View>  curFixedViews;
    
        void Awake()
        {
            if (__instance == null)
            {
                __instance = this;
                //跨越场景时不销毁
                GameObject.DontDestroyOnLoad(this.gameObject);
    
                curViews = new Dict<string, View>();
                curFixedViews = new Dict<string, View>();
            }
        }
        
        /// <summary>
        /// 打开一个View
        /// </summary>
        public void Open(string rViewName, ViewData.State rViewState, Action<View> rOpenCompleted = null)
        {
            // 企图关闭当前的View
            MaybeCloseTopView(rViewState);

            this.StartCoroutine(Open_Async(rViewName, rViewState, rOpenCompleted));
        }

        public Coroutine OpenAsync(string rViewName, ViewData.State rViewState, Action<View> rOpenCompleted = null)
        {
            return this.StartCoroutine(Open_Async(rViewName, rViewState, rOpenCompleted));
        }

        private IEnumerator Open_Async(string rViewName, ViewData.State rViewState, Action<View> rOpenCompleted)
        {
            var rLoaderRequest = UILoader.Instance.LoadUI(rViewName);
            yield return rLoaderRequest.Coroutine;

            OpenView(rLoaderRequest.ViewPrefabGo, rViewState, rOpenCompleted);
        }
    
        /// <summary>
        /// 移除顶层View
        /// </summary>
        public void Pop(Action rCloseComplted = null)
        {
            // 得到顶层结点
            KeyValuePair<string, View> rTopNode = this.curViews.Last();
    
            string rViewGUID = rTopNode.Key;
            View rView = rTopNode.Value;
    
            if (rView == null)
            {
                UtilTool.SafeExecute(rCloseComplted);
                return;
            }
    
            // 移除顶层结点
            this.curViews.Remove(rViewGUID);
            rView.Close();
            this.StartCoroutine(DestroyView_Async(rView, () => 
            {
                UtilTool.SafeExecute(rCloseComplted);
            }));
        }
    
        /// <summary>
        /// 根据GUID来关闭指定的View
        /// </summary>
        public void CloseView(string rViewGUID, Action rCloseCompleted = null)
        {
            bool isFixedView = false;
            View rView = null;
    
            // 找到View
            if (this.curFixedViews.TryGetValue(rViewGUID, out rView))
            {
                isFixedView = true;
            }
            else if (this.curViews.TryGetValue(rViewGUID, out rView))
            {
                isFixedView = false;
            }
            
            // View不存在
            if (rView == null)
            {
                UtilTool.SafeExecute(rCloseCompleted);
                return;
            }
    
            // View存在
            if (isFixedView)
            {
                this.curFixedViews.Remove(rViewGUID);
            }
            else
            {
                this.curViews.Remove(rViewGUID);
            }
    
            // 移除顶层结点
            rView.Close();
            this.StartCoroutine(DestroyView_Async(rView, () =>
            {
                UtilTool.SafeExecute(rCloseCompleted);
            })); 
        }
    
        /// <summary>
        /// 初始化View，如果是Dispatch类型的话，只对curViews顶层View进行交换
        /// </summary>
        private void OpenView(GameObject rViewPrefab, View.State rViewState, Action<View> rOpenCompleted)
        {
            if (rViewPrefab == null) return;
    
            //把View的GameObject结点加到rootCanvas下
            GameObject rViewGo = this.rootCanvas.transform.AddChild(rViewPrefab, "UI");
    
            View rView = rViewGo.SafeGetComponent<View>();
            if (rView == null)
            {
                Debug.LogErrorFormat("GameObject {0} has not View script.", rViewGo.name);
                UtilTool.SafeExecute(rOpenCompleted, null);
                return;
            }
    
            //生成GUID
            string rViewGUID = Guid.NewGuid().ToString();
    
            //为View的初始化设置
            rView.Initialize(rViewGUID, rViewState);
    
            //新的View的存储逻辑
            switch (rView.curState)
            {
                case View.State.fixing:
                    curFixedViews.Add(rViewGUID, rView);
                    break;
                case View.State.overlap:
                    curViews.Add(rViewGUID, rView);
                    break;
                case View.State.dispatch:
                    if (curViews.Count == 0)
                        curViews.Add(rViewGUID, rView);
                    else 
                        curViews[curViews.LastKey()] = rView;
                    break;
                default:
                    break;
            }
    
            rView.Open(rOpenCompleted);
        }
    
        /// <summary>
        /// 企图关闭一个当前的View，当存在当前View时候，并且传入的View是需要Dispatch的。
        /// </summary>
        private void MaybeCloseTopView(ViewData.State rViewState)
        {
            // 得到顶层结点
            KeyValuePair<string, View> rTopNode = this.curViews.Last();
    
            string rViewGUID = rTopNode.Key;
            View rView = rTopNode.Value;
    
            if (rView == null) return;
    
            if (rViewState == ViewData.State.dispatch)
            {
                // 移除顶层结点
                this.curViews.Remove(rViewGUID);
                rView.Close();
                this.StartCoroutine(DestroyView_Async(rView));
            }
        }
    
        /// <summary>
        /// 等待View关闭动画播放完后开始删除一个View
        /// </summary>
        private IEnumerator DestroyView_Async(View rView, Action rDestroyCompleted = null)
        {
            while (!rView.IsClosed)
            {
                yield return 0;
            }
    
            GameObject.DestroyObject(rView.gameObject);
    
            UtilTool.SafeExecute(rDestroyCompleted);
        }
    }
}

