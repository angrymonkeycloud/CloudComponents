using AngryMonkey.Cloud.Components;

namespace AngryMonkey.Cloud.Components;

public class CloudWeb2
{
    public CloudPage PageDefaults { get; set; } = new();
    public string TitlePrefix { get; set; } = string.Empty;
    public string TitleSuffix { get; set; } = string.Empty;
    public static string[] CrawlersUserAgents { get; } = new[]
    {
        "bot","crawler","spider","80legs","baidu","yahoo! slurp","ia_archiver","mediapartners-google",
        "lwp-trivial","nederland.zoek","ahoy","anthill","appie","arale","araneo","ariadne",
        "atn_worldwide","atomz","bjaaland","ukonline","calif","combine","cosmos","cusco",
        "cyberspyder","digger","grabber","downloadexpress","ecollector","ebiness","esculapio",
        "esther","felix ide","hamahakki","kit-fireball","fouineur","freecrawl","desertrealm",
        "gcreep","golem","griffon","gromit","gulliver","gulper","whowhere","havindex","hotwired",
        "htdig","ingrid","informant","inspectorwww","iron33","teoma","ask jeeves","jeeves",
        "image.kapsi.net","kdd-explorer","label-grabber","larbin","linkidator","linkwalker",
        "lockon","marvin","mattie","mediafox","merzscope","nec-meshexplorer","udmsearch","moget",
        "motor","muncher","muninn","muscatferret","mwdsearch","sharp-info-agent","webmechanic",
        "netscoop","newscan-online","objectssearch","orbsearch","packrat","pageboy","parasite",
        "patric","pegasus","phpdig","piltdownman","pimptrain","plumtreewebaccessor","getterrobo-plus",
        "raven","roadrunner","robbie","robocrawl","robofox","webbandit","scooter","search-au",
        "searchprocess","senrigan","shagseeker","site valet","skymob","slurp","snooper","speedy",
        "curl_image_client","suke","www.sygol.com","tach_bw","templeton","titin","topiclink","udmsearch",
        "urlck","valkyrie libwww-perl","verticrawl","victoria","webscout","voyager","crawlpaper",
        "webcatcher","t-h-u-n-d-e-r-s-t-o-n-e","webmoose","pagesinventory","webquest","webreaper",
        "webwalker","winona","occam","robi","fdse","jobo","rhcs","gazz","dwcp","yeti","fido","wlm",
        "wolp","wwwc","xget","legs","curl","webs","wget","sift","cmc", "008/0.83","192.comAgent","1on1searchBot",
        "1st Choice Spider","1stSpider","2dehands.be Bot","2ip.ru CMS Detector","360Spider","404checker","A6-Indexer",
        "AASP","AbachoBOT","Abonti","AboutUsBot","Accoona-AI-Agent","Accoona-Biz-Agent","Accoona-Conv-Agent","Accoona-Dir-Agent",
        "Accoona-Image-Agent","Accoona-Person-Agent","Accoona-Prod-Agent","Ace Explorer","Achims-Robot","ActiveBookmark","Adamm Bot",
        "AddressOrganizer","AhrefsBot","AIBOT","aiHitBot","Aipbot","AISIID","Akamai-SiteSnapshot","Alertbot",
        "Alexa Web Search Platform","AlexfDownload","Alexibot","AlkalineBOT","All Academic","AlltheWeb","AlphaBot","Amfibibot",
        "AmiNET","Amorank Spider","AmphetaDesk","AnnoMille spider","Anonymized by ProxyOS","AnswerBus","AnswerChase PROve",
        "AnswerPail","AntBot","Antibot","Antro.Net","AnzwersCrawl","AONDE-Spider","Aport","Aqua_Products","AraBot","Arachmo",
        "Arachnophilia","archive.org_bot","Arquivo-web-crawler","ASAHA Search Engine Turkey","Asahina-Antenna","ask.24x.info",
        "Ask24","AskBar","AskJeeves","AskJeeves-Testing","ASPSeek","Astalavista.box.sk Crawler","ATHENS","AtlocalBot",
        "Atomic_Email_Hunter","attach","attrakt","Attributor","AURESYS","AUSTRALIA-CRAWL","AutoBaron","autoemailspider","Autonomy",
        "Avant Browser","AVSearch-","Axonize-bot","Ayna","B-l-i-t-z-B-O-T","BackDoorBot","BackStreet Browser","BackWeb","Badass",
        "Baiduspider","BaliBot","BannanaBot","BarraHomeCrawler","BDFetch","BDFetch.bot","BecomeBot","BeetleBot",
        "Bender In-Depth Crawler","besserscheitern-crawl","betaBot","Big Brother","Bigado.com","BigCliqueBot","Bigfoot",
        "BigWebDirectory.com","Bilbo","BilgiBetaBot","binlar","Bingbot","BinGet","Bintellibot","bitlybot",
        "BizBot04 kirk.overleaf.com","BizBot04 Piotr","BizBot04 samurajdata.se","BizBot04 samurajdata.se(kirk)",
        "BizBot04 samurajdata.se(Piotr)","BizBot04 Ultrascan","Bizzocchi","Black Hole","BlackWidow","Bladder fusion","Blaiz-Bee",
        "Blaiz-Bee","Blaiz-Bee","Blaiz-Bee","BLEXBot","BlitzBOT","Blog conversation project","BlogMyWay","BlogPulseLive",
        "BlogRefsBot","BlogScope","Blogslive","Blogvani bot","bloobybot","BlowFish","BlowFish","BlowFish","BlowFish","BlowFish",
        "BLT","bnf.fr_bot","boitho.com-dc","Boitho.com-robot","Booster","Bot Apoena","BotALot","botao","BOTW Spider","bRAT",
        "Browsershots","BSDSeek","BTbot","BuiltBotTough",
    };
}
