using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Threading.Tasks;

namespace VodaBoopMobile
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        bool loading;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            TextView dateInput = FindViewById<TextView> (Resource.Id.date);
            TextView teamInput = FindViewById<TextView> (Resource.Id.searchTeam);
            Button submitButton = FindViewById<Button> (Resource.Id.submitButton);
            TextView fixtureResult = FindViewById<TextView> (Resource.Id.result);
            View loadingBar = FindViewById (Resource.Id.loadingBar);

            dateInput.TextChanged += (s, a) =>
            {
                fixtureResult.Text = string.Empty;
                teamInput.Text = string.Empty;
            };
            teamInput.TextChanged += (s, a) =>
            {
                fixtureResult.Text = string.Empty;
            };
            submitButton.Click += (s, a) =>
            {
                if (loading)
                    return;
                if (dateInput.Text.Length == 6 && teamInput.Text.Length > 0)
                {
                    loading = true;
                    loadingBar.Visibility = ViewStates.Visible;
                    GetFixture (dateInput.Text, teamInput.Text,(fixture)=>
                    {
                        loading = false;
                        if (fixture == null)
                        {
                            fixtureResult.Text = "No Result";
                        }
                        else
                        {
                            string fullTimeScore = fixture.score.fulltime;
                            int score1 = int.Parse (fullTimeScore[0].ToString ());
                            int score2 = int.Parse (fullTimeScore[2].ToString ());

                            string result = score1 > score2 ? "1" : score1 == score2 ? "x" : "2";

                            fixtureResult.Text = $"Answer is {result}";
                        }
                        loadingBar.Visibility = ViewStates.Gone;
                    });
                }
                else
                {
                    Toast.MakeText (this, "Invalid Inputs", ToastLength.Long).Show ();
                }
            };
            loadingBar.Visibility = ViewStates.Gone;
            fixtureResult.Text = "Enter Info";
        }

        void GetFixture (string dateInput, string team, Action<Fixture> callback)
        {
            Task.Run (() =>
            {
                try
                {
                    string date = $"20{dateInput.Substring (4, 2)}-{dateInput.Substring (2, 2)}-{dateInput.Substring (0, 2)}";

                    //eg
                    //181116
                    //to
                    //2016-11-18

                    string teamId = GetTeamId (team);

                    var client = new RestClient ($"https://api-football-v1.p.rapidapi.com/v2/fixtures/date/{date}");
                    var request = new RestRequest (Method.GET);
                    request.AddHeader ("x-rapidapi-host", "api-football-v1.p.rapidapi.com");
                    request.AddHeader ("x-rapidapi-key", "adf90acd9dmshe60ab608ee7dbcap12fee5jsnad683ee8217a");
                    IRestResponse response = client.Execute (request);

                    if(!response.IsSuccessful)
                    {
                        RunOnUiThread (() => callback.Invoke (null));
                        return;
                    }    
                    ApiFixtureResponse _response = JsonConvert.DeserializeObject<ApiFixtureResponse> (response.Content);

                    RunOnUiThread (() => callback.Invoke (WithTeam (teamId, _response.api) ?? null));
                }
                catch
                {
                    RunOnUiThread (() => callback.Invoke (null));
                }
            });
        }
        static Fixture WithTeam (string teamId, ApiFixture obj)
        {
            foreach (Fixture fixture in obj.fixtures)
            {
                string a = fixture.homeTeam.team_id;
                string b = fixture.awayTeam.team_id;

                if (a == teamId || b == teamId)
                {
                    return fixture;
                }
            }
            return null;
        }
        static string GetTeamId (string searchString)
        {
            searchString = searchString.Replace (" ", "_").ToLower ();

            var client = new RestClient ($"https://api-football-v1.p.rapidapi.com/v2/teams/search/{searchString}");
            var request = new RestRequest (Method.GET);
            request.AddHeader ("x-rapidapi-host", "api-football-v1.p.rapidapi.com");
            request.AddHeader ("x-rapidapi-key", "adf90acd9dmshe60ab608ee7dbcap12fee5jsnad683ee8217a");
            IRestResponse response = client.Execute (request);

            ApiTeamResponse resp = JsonConvert.DeserializeObject<ApiTeamResponse> (response.Content);
            return (resp.api.results == 0) ? null : resp.api.teams[0].team_id;
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
        public class ApiFixtureResponse
        {
            public ApiFixture api;
        }
        public class ApiFixture
        {
            public int results;
            public Fixture[] fixtures;
        }
        public class Fixture
        {
            public Score score;
            public Team homeTeam;
            public Team awayTeam;
        }
        public class ApiTeamResponse
        {
            public ApiTeam api;
        }
        public class ApiTeam
        {
            public int results;
            public Team[] teams;
        }
        public class Team
        {
            public string team_id;
        }
        public class Score
        {
            public string fulltime;
        }

    }
}