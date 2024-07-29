using System;
using System.Web;
using System.Threading.Tasks;
using Microsoft.Playwright;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace medicare_login
{
    class Program
    {
		    
		    // Web에서 인코딩 상태로 넘어온 URI 값을 디코딩 후 역직렬화 하여 
		    // 타이틀 값을 받아오는 코드
		    
        public class JsonStringSet
        {
		        // 공인인증서, 비밀번호, 약국ID 속성 지정
		        // 공인인증서 속성
            public string? title { get; set; }
            // 비밀번호 속성
            public string? password { get; set; }
            // 약국ID 속성
            public string? subTitle { get; set; }
        }
        public static async Task Main(string[] args)
        {
            // Web에서 넘어온 인자 수 확인
            Console.WriteLine($"Number of args: {args.Length}");
            if (args.Length > 0)
            {
                // 첫 번째 인자 출력
                Console.WriteLine($"First arg: {args[0]}");
            }
            else
            {
                Console.WriteLine("No URI provided.");
                return;
            }
            string uri = args[0];
            Console.WriteLine($"Received URI: {uri}");
            // URI 디코딩
            string decodedUri = HttpUtility.UrlDecode(uri);
            Console.WriteLine($"Decoded URI: {decodedUri}");
            // 문자열 추출
            string jsonPattern = @"\{.*\}";
            Match match = Regex.Match(decodedUri, jsonPattern);
            string jsonString = match.Value;
            Console.WriteLine($"Json pattern Uri : {jsonString}");
            // 역직렬화 작업
            JsonStringSet? data = JsonSerializer.Deserialize<JsonStringSet>(jsonString);
            Console.WriteLine($"title:{data?.title}");
            Console.WriteLine($"password:{data?.password}");
            Console.WriteLine($"subTitle:{data?.subTitle}");
            // 각 속성에 역직렬화한 타이틀 값을 대입
            string title = data?.title;
            string password = data?.password;
            string subTitle = data?.subTitle;
            
            //////////////////////////////////////////////////////////////
            
            // 브라우저 선택 코드
            
            using var playwright = await Playwright.CreateAsync();
            IBrowser browser = null;
            try
            {
	              // 기본적으로는 Chrone 브라우저를 통해 실행
                var options = new BrowserTypeLaunchOptions
                {
                    Channel = "chrome",
                    Headless = false
                };
                browser = await playwright.Chromium.LaunchAsync(options);
                Console.WriteLine("Chrome 브라우저 실행");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chrome 브라우저가 없습니다.: {ex.Message}");
                try
                {
		                // 만일 Chrome 브라우저가 없다면 Edge 브라우저 실행
                    var options = new BrowserTypeLaunchOptions
                    {
                        Channel = "msedge",
                        Headless = false
                    };
                    browser = await playwright.Chromium.LaunchAsync(options);
                    Console.WriteLine("msedge 브라우저 실행");
                }
                catch (Exception ex2)
                {
                    Console.WriteLine($"msedge 브라우저가 없습니다.: {ex2.Message}");
                    return;
                }
            }
            // Chrome, Edge 브라우저가 모두 없다면 실행
            if (browser == null)
            {
                Console.WriteLine("모든 브라우저가 없습니다.");
                return;
            }
            var page = await browser.NewPageAsync();

            //////////////////////////////////////////////////////////////
            
            // 자동 로그인 코드

            // 요양기관정보마당 홈페이지 접속
            await page.GotoAsync("https://medicare.nhis.or.kr/portal/index.do");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            // 로그인 버튼 클릭
            await page.ClickAsync("#grp_loginBtn");
            Console.WriteLine("로그인 버튼 클릭");
            // 인증서로그인 버튼 클릭
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.ClickAsync("#btnCorpLogin");
            Console.WriteLine("인증서로그인 버튼 클릭");
            // 하드디스크 버튼 클릭
            await Task.Delay(1000);
            await page.WaitForSelectorAsync("#xwup_media_hdd");
            await page.ClickAsync("#xwup_media_hdd");
            Console.WriteLine("하드디스크 버튼 클릭");
            // 텍스트로 검색하여 공인인증서 클릭
            // Web을 통해 받아온 'title' 값을 대입하여 `targetRowSelector_medi`에 값을 넣음
            var targetRowSelector_medi = $"tr:has-text('{title}')";
            await page.WaitForSelectorAsync(targetRowSelector_medi);
            var targetRow_public = await page.QuerySelectorAsync(targetRowSelector_medi);
            if (targetRow_public != null)
            {
                await page.WaitForTimeoutAsync(1000);
                await targetRow_public.ClickAsync();
                Console.WriteLine("공인인증서 선택 완료");
            }
            else
            {
                Console.WriteLine("공인인증서를 찾을 수 없습니다.");
            }
            // 텍스트로 검색하여 비밀번호 입력
            await page.FillAsync("#xwup_certselect_tek_input1", password);
            Console.WriteLine("비밀번호 입력");
            // 확인 버튼 클릭
            await page.ClickAsync("#xwup_OkButton");
            Console.WriteLine("확인 버튼 클릭");
            // 텍스트로 검색하여 약국ID 클릭
            var targetRowSelector_id = $"tr:has-text('{subTitle}')";
            await page.WaitForSelectorAsync(targetRowSelector_id);
            var targetRow_medi = await page.QuerySelectorAsync(targetRowSelector_id);
            if (targetRow_medi != null)
            {
                var loginLink = await targetRow_medi.QuerySelectorAsync(".login");
                if (loginLink != null)
                {
                    await loginLink.ClickAsync();
                    Console.WriteLine("약국 선택 완료");
                }
                else
                {
                    Console.WriteLine("약국을 찾을 수 없습니다.");
                }
            }
            else
            {
                Console.WriteLine("약국을 찾을 수 없습니다.");
            }
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            //3초 후 자동으로 종료
            await Task.Delay(3000);
            await browser.CloseAsync();
        }
    }
}
