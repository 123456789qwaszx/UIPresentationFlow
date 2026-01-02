using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UIScreenSpecAsset", menuName = "UI/UIScreenSpecAsset")]
public sealed class UIScreenSpecAsset : ScriptableObject
{
    public UIScreenSpec spec = new ();
}