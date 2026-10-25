using System.Globalization;

namespace BlackHole.Unity
{
    // 데모 흐름이 쓰는 글자 표 키(BlackHoleDemoText.csv). 게임의 UIText에서 이 화면들에 쓰는 것만 남기고, 데모 전용 글자(demo.*)를 더했다.
    // 화면 프리팹의 글자({화면 타입 이름}.{Ref 이름})는 ViewTexts가 넣으므로 여기에 없다.
    internal static class DemoText
    {
        public static class Mode
        {
            public const string NormalTitle = "mode.normal.title";
            public const string NormalDescription = "mode.normal.description";
        }

        public const string Cancel = "confirm.cancel";

        public static class Demo
        {
            public const string Hint = "demo.hint";
            public const string NewGameTitle = "demo.newGame.title";
            public const string NewGameBody = "demo.newGame.body";
            public const string NewGameConfirm = "demo.newGame.confirm";
            public const string ContinueNotice = "demo.continue.notice";
            public const string MainMenuTitle = "demo.mainMenu.title";
            public const string MainMenuBody = "demo.mainMenu.body";
            public const string MainMenuConfirm = "demo.mainMenu.confirm";
            public const string QuitTitle = "demo.quit.title";
            public const string QuitBody = "demo.quit.body";
            public const string QuitConfirm = "demo.quit.confirm";
        }

        public static class Setting
        {
            // 설정 이름: setting.{설정 ID}
            public static string Label(string settingId) => "setting." + settingId;

            // 설정 선택지: setting.{설정 ID}.{번호}
            public static string Option(string settingId, int index) => "setting." + settingId + "." + Number(index);
        }

        public static class Ending
        {
            public const string CreditsTitle = "ending.credits.title";
            public const string Thanks = "ending.thanks";
            public const string Prompt = "ending.prompt";

            public static string Line(int index) => "ending.line." + Number(index);
            public static string CreditRole(int index) => "ending.credits." + Number(index) + ".role";
            public static string CreditNames(int index) => "ending.credits." + Number(index) + ".names";
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
