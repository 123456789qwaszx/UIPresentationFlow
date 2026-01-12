// using UnityEngine;
//
// public class UITestDriver : MonoBehaviour
// {
//     [SerializeField] private UIBootStrap bootstrap;
//     private UIOpener _uiOpener;
//
//     [SerializeField] private int curGold = 555;
//     [SerializeField] private int curHp = 666;
//     [SerializeField] private int maxHp = 777;
//     [SerializeField] private int curGem = 888;
//     
//     void Start()
//     {
//         if (bootstrap == null)
//             bootstrap = FindFirstObjectByType<UIBootStrap>();
//         
//         _uiOpener = bootstrap.Opener;
//     }
//     
//     public void OnOpenHome()
//     {
//         _uiOpener.Open(DefaultActionKeys.OpenHome);
//     }
//
//     public void OnOpenShop()
//     {
//         _uiOpener.Open(DefaultActionKeys.OpenShop);
//         _uiOpener.SetGold(curGold);
//         _uiOpener.SetHp(curHp, maxHp);
//         _uiOpener.SetGold(curGem);
//     }
// }
