namespace Nontiwat_7362_L002.ViewModels;

// Names and provinces sourced from Agoda on 2026-11-10. Rates and photos are demo content.
public record HotelCardViewModel(string Id, string Name, string City, string ImageKey, string SourceUrl, int Price)
{
    public Nontiwat_7362_L002.Models.StayEnvironment Environment { get; init; } = new();
    public string? PhotoUrl { get; init; }
    public string? PhotoSourceUrl { get; init; }
    public string? PhotoCredit { get; init; }
    public string ImageUrl => PhotoUrl ?? "/images/stay/photo-pending.svg";
    public IReadOnlyList<string> Themes { get; init; } = [];
    // Average guest rating on a 1–5 scale; null when the hotel has no reviews yet.
    public double? ReviewScore { get; init; }
    public int ReviewCount { get; init; }
    public string? PublicDescription { get; init; }
    public decimal? StartingRate { get; init; } = Price;
    public IReadOnlyList<string>? Amenities { get; init; }
    public string Description => PublicDescription ?? $"ที่พักในจังหวัด{City} เลือกวันและห้องพักเพื่อทดลองใช้งานระบบจองของ Lodgo";
}

public static class HotelCatalog
{
    public const string SourcesCheckedOn = "2026-11-10";
    public static IReadOnlyList<HotelCardViewModel> All { get; } = Array.AsReadOnly<HotelCardViewModel>(
    [
        new("th-stay-001", "Amari Don Muang Airport Bangkok Hotel", "กรุงเทพมหานคร", "urban", "https://www.agoda.com/en-us/amari-don-muang-airport-bangkok-hotel/hotel/bangkok-th.html", 1200),
        new("th-stay-002", "Baan Ing Daan", "อำนาจเจริญ", "mountain", "https://www.agoda.com/en-au/city/amnat-charoen-th.html", 1450),
        new("th-stay-003", "Suphorn Grand Hotel", "อ่างทอง", "river", "https://www.agoda.com/theme/free-cancellation/city/ang-thong-th.html", 1700),
        new("th-stay-004", "The One Hotel", "บึงกาฬ", "garden", "https://www.agoda.com/id-id/region/bueng-kan-province-th.html", 1950),
        new("th-stay-005", "Amari Buriram United", "บุรีรัมย์", "forest", "https://www.agoda.com/cs-cz/theme/resorts/city/buriram-th.html", 2200),
        new("th-stay-006", "Heaven Hotel Chachoengsao", "ฉะเชิงเทรา", "nan", "https://www.agoda.com/heaven-hotel-chachoengsao/hotel/chachoengsao-th.html", 2450),
        new("th-stay-007", "Chaophayathara Riverside Hotel", "ชัยนาท", "urban", "https://www.agoda.com/en-in/hotels-near-chai-nat-hospital/attractions/chai-nat-th.html", 2700),
        new("th-stay-008", "Siam River Resort", "ชัยภูมิ", "mountain", "https://www.agoda.com/city/chaiyaphum-th.html", 2950),
        new("th-stay-009", "Maneechan Resort", "จันทบุรี", "river", "https://www.agoda.com/theme/agoda-home/city/chanthaburi-th.html", 1200),
        new("th-stay-010", "U Nimman Chiang Mai", "เชียงใหม่", "garden", "https://www.agoda.com/th-th/u-nimman-chiang-mai/hotel/chiang-mai-th.html", 1450),
        new("th-stay-011", "Le Méridien Chiang Rai Resort, Thailand", "เชียงราย", "forest", "https://www.agoda.com/destination/city/chiang-rai-th.html", 1700),
        new("th-stay-012", "Grande Centre Point Pattaya", "ชลบุรี", "sunset", "https://www.agoda.com/en-us/grande-centre-point-pattaya/hotel/pattaya-th.html", 1950),
        new("th-stay-013", "Nana Beach Hotel", "ชุมพร", "urban", "https://www.agoda.com/en-us/nana-beach-hotel/hotel/chumphon-th.html", 2200),
        new("th-stay-014", "A.P Garden Hotel", "กาฬสินธุ์", "mountain", "https://www.agoda.com/city/kalasin-th.html", 2450),
        new("th-stay-015", "HOP INN Kamphaeng Phet", "กำแพงเพชร", "river", "https://www.agoda.com/hop-inn-kamphaeng-phet/hotel/kamphaeng-phet-th.html", 2700),
        new("th-stay-016", "U Inchantree Kanchanaburi Hotel", "กาญจนบุรี", "garden", "https://www.agoda.com/u-inchantree-kanchanaburi-hotel/hotel/kanchanaburi-th.html", 2950),
        new("th-stay-017", "The Heritage Grand Khon Kaen Hotel & Convention", "ขอนแก่น", "forest", "https://www.agoda.com/en-sg/pullman-khon-kaen-raja-orchid-hotel/hotel/khon-kaen-th.html", 1200),
        new("th-stay-018", "Panan Krabi Resort", "กระบี่", "huahin", "https://www.agoda.com/en-us/panan-krabi-resort_2/hotel/krabi-th.html", 1450),
        new("th-stay-019", "The Coconut Hotel", "ลำปาง", "urban", "https://www.agoda.com/en-in/hotels-near-home-at-lampang/attractions/lampang-th.html", 1700),
        new("th-stay-020", "Siri Hotel Lumphun", "ลำพูน", "mountain", "https://www.agoda.com/en-in/city/lamphun-th.html", 1950),
        new("th-stay-021", "Loei Palace Hotel", "เลย", "river", "https://www.agoda.com/en-ie/city/loei-th.html", 2200),
        new("th-stay-022", "HOP INN Lopburi", "ลพบุรี", "garden", "https://www.agoda.com/region/lop-buri-province-th.html", 2450),
        new("th-stay-023", "Pai Village Boutique Resort", "แม่ฮ่องสอน", "forest", "https://www.agoda.com/en-sg/region/mae-hong-son-province-th.html", 2700),
        new("th-stay-024", "Taksila Hotel", "มหาสารคาม", "nan", "https://www.agoda.com/region/maha-sarakham-province-th.html", 2950),
        new("th-stay-025", "Riverfront Hotel Mukdahan", "มุกดาหาร", "urban", "https://www.agoda.com/city/mukdahan-th.html", 1200),
        new("th-stay-026", "Royal Hills Golf Resort and Spa", "นครนายก", "mountain", "https://www.agoda.com/royal-hills-golf-resort-and-spa/hotel/nakhon-nayok-th.html", 1450),
        new("th-stay-027", "Mida Grande Hotel Dhavaravati Nakhon Pathom", "นครปฐม", "river", "https://www.agoda.com/mida-grande-hotel-dhavaravati-nakhon-pathom/hotel/nakhon-pathom-th.html", 1700),
        new("th-stay-028", "Fortune River View Hotel Nakhon Phanom", "นครพนม", "garden", "https://www.agoda.com/fortune-river-view-hotel-nakhon-phanom/hotel/nakhonpanom-th.html", 1950),
        new("th-stay-029", "Centre Point Hotel Terminal21 Korat", "นครราชสีมา", "forest", "https://www.agoda.com/en-us/hotels-near-the-mall-ratchasima/attractions/nakhonratchasima-th.html", 2200),
        new("th-stay-030", "P.A. Thani Hotel", "นครสวรรค์", "nan", "https://www.agoda.com/p-a-thani-hotel/hotel/nakhon-sawan-th.html", 2450),
        new("th-stay-031", "Grand Fortune Hotel Nakhon Si Thammarat", "นครศรีธรรมราช", "urban", "https://www.agoda.com/grand-fortune-hotel-nakhon-si-thammart/hotel/nakhon-si-thammarat-th.html", 2700),
        new("th-stay-032", "Nan Seasons Boutique Resort", "น่าน", "mountain", "https://www.agoda.com/nan-seasons-boutique-resort/hotel/nan-th.html", 2950),
        new("th-stay-033", "Imperial Narathiwat Hotel", "นราธิวาส", "river", "https://www.agoda.com/en-in/imperial-narathiwat-hotel/hotel/narathiwat-th.html", 1200),
        new("th-stay-034", "Nong Bua Greenview Hotel", "หนองบัวลำภู", "garden", "https://www.agoda.com/en-sg/city/nong-bua-lam-phu-th.html", 1450),
        new("th-stay-035", "Amanta Hotel Nongkhai", "หนองคาย", "forest", "https://www.agoda.com/en-ie/nongkhai-city-center/maps/nong-khai-th.html", 1700),
        new("th-stay-036", "Oakwood Suites Tiwanon Bangkok", "นนทบุรี", "nan", "https://www.agoda.com/oakwood-suites-tiwanon-bangkok/hotel/bangkok-th.html", 1950),
        new("th-stay-037", "Novotel Bangkok Future Park Rangsit", "ปทุมธานี", "urban", "https://www.agoda.com/rangsit/maps/bangkok-th.html", 2200),
        new("th-stay-038", "C.S. Pattani Hotel", "ปัตตานี", "mountain", "https://www.agoda.com/c-s-pattani-hotel_2/hotel/pattani-th.html", 2450),
        new("th-stay-039", "The Sands Khao Lak by Katathani Resort", "พังงา", "pattaya", "https://www.agoda.com/the-sands-khao-lak-by-katathani-resort/hotel/khao-lak-th.html", 2700),
        new("th-stay-040", "Sripakpra Boutique Resort Phatthalung", "พัทลุง", "garden", "https://www.agoda.com/city/phatthalung-th.html", 2950),
        new("th-stay-041", "Phayao Gateway Hotel", "พะเยา", "forest", "https://www.agoda.com/region/phayao-province-th.html", 1200),
        new("th-stay-042", "Imperial Phukaew Hill Resort", "เพชรบูรณ์", "nan", "https://www.agoda.com/region/phetchabun-province-th.html", 1450),
        new("th-stay-043", "SO/ Sofitel Hua Hin", "เพชรบุรี", "huahin", "https://www.agoda.com/en-us/so-sofitel-hua-hin/hotel/hua-hin-cha-am-th.html", 1700),
        new("th-stay-044", "The Nest Hotel Phichit", "พิจิตร", "mountain", "https://www.agoda.com/en-gb/region/phichit-province-th.html", 1950),
        new("th-stay-045", "Topland Hotel", "พิษณุโลก", "river", "https://www.agoda.com/th-th/phitsanulok-city-center/maps/phitsanulok-th.html", 2200),
        new("th-stay-046", "Krungsri River Hotel", "พระนครศรีอยุธยา", "garden", "https://www.agoda.com/en-in/hotels-near-civilize-ayutthaya/attractions/ayutthaya-th.html", 2450),
        new("th-stay-047", "Hug Inn Phrae Hotel", "แพร่", "forest", "https://www.agoda.com/en-sg/hug-inn-phrae-hotel/hotel/phrae-th.html", 2700),
        new("th-stay-048", "The Nai Harn", "ภูเก็ต", "huahin", "https://www.agoda.com/the-nai-harn/hotel/phuket-th.html", 2950),
        new("th-stay-049", "Kantary Hotel Kabinburi", "ปราจีนบุรี", "urban", "https://www.agoda.com/kantary-hotel-kabinburi/hotel/prachinburi-th.html", 1200),
        new("th-stay-050", "Hyatt Regency Hua Hin", "ประจวบคีรีขันธ์", "samui", "https://www.agoda.com/the-barai-suites-and-spa-at-hyatt-regency-hua-hin/hotel/hua-hin-cha-am-th.html", 1450),
        new("th-stay-051", "Tinidee Hotel @ Ranong.", "ระนอง", "river", "https://www.agoda.com/en-ca/tinidee-hotel-ranong-h69589790/hotel/ranong-th.html", 1700),
        new("th-stay-052", "Navela Hotel & Convention", "ราชบุรี", "garden", "https://www.agoda.com/theme/business/city/ratchaburi-th.html", 1950),
        new("th-stay-053", "Holiday Inn & Suites Rayong City Centre By IHG", "ระยอง", "huahin", "https://www.agoda.com/holiday-inn-suites-rayong-city-centre/hotel/rayong-th.html", 2200),
        new("th-stay-054", "Mgrandhotel", "ร้อยเอ็ด", "nan", "https://www.agoda.com/mgrandhotel/hotel/roi-et-th.html", 2450),
        new("th-stay-055", "Indochina Hotel", "สระแก้ว", "urban", "https://www.agoda.com/indochina-hotel_7/hotel/sa-kaeo-th.html", 2700),
        new("th-stay-056", "Dusit Hotel", "สกลนคร", "mountain", "https://www.agoda.com/destination/city/sakon-nakhon-th.html", 2950),
        new("th-stay-057", "Eastin Thana City Golf Resort Bangkok", "สมุทรปราการ", "river", "https://www.agoda.com/en-ca/hotels-near-summit-windmill-golf-club/attractions/bangkok-th.html", 1200),
        new("th-stay-058", "The Eight", "สมุทรสาคร", "garden", "https://www.agoda.com/vi-vn/city/samut-sakhon-th.html", 1450),
        new("th-stay-059", "Baan Amphawa Resort and Spa", "สมุทรสงคราม", "forest", "https://www.agoda.com/city/amphawa-samut-songkhram-th.html", 1700),
        new("th-stay-060", "Glai Gan Place Hotel", "สระบุรี", "nan", "https://www.agoda.com/en-us/glai-gan-place-hotel/hotel/saraburi-th.html", 1950),
        new("th-stay-061", "Bundhaya Resort", "สตูล", "ocean", "https://www.agoda.com/bundhaya-resort/hotel/koh-lipe-th.html", 2200),
        new("th-stay-062", "Chaisaeng Palace Hotel", "สิงห์บุรี", "mountain", "https://www.agoda.com/chaisaeng-palace-hotel/hotel/sing-buri-th.html", 2450),
        new("th-stay-063", "Srilamduan Hotel", "ศรีสะเกษ", "river", "https://www.agoda.com/srilamduan-hotel/hotel/sisaket-th.html", 2700),
        new("th-stay-064", "Centara Hotel Hat Yai", "สงขลา", "garden", "https://www.agoda.com/centara-hotel-hat-yai/hotel/hat-yai-th.html", 2950),
        new("th-stay-065", "Sriwilai Sukhothai", "สุโขทัย", "forest", "https://www.agoda.com/th-th/hotels-near-wat-phra-pai-luang/attractions/sukhothai-th.html", 1200),
        new("th-stay-066", "Songphanburi Hotel", "สุพรรณบุรี", "nan", "https://www.agoda.com/en-in/suphan-buri/maps/suphan-buri-th.html", 1450),
        new("th-stay-067", "Samui Buri Beach Resort", "สุราษฎร์ธานี", "sunset", "https://www.agoda.com/samui-buri-beach-resort/hotel/koh-samui-th.html", 1700),
        new("th-stay-068", "Thongtarin Hotel", "สุรินทร์", "mountain", "https://www.agoda.com/thongtarin-hotel/hotel/surin-th.html", 1950),
        new("th-stay-069", "Viangtak Riverside Hotel", "ตาก", "river", "https://www.agoda.com/viangtak-riverside-hotel/hotel/tak-th.html", 2200),
        new("th-stay-070", "Rua Rasada Express", "ตรัง", "garden", "https://www.agoda.com/pt-pt/budget-hotel-by-rua-rasada/hotel/trang-th.html", 2450),
        new("th-stay-071", "KC Grande Resort Koh Chang", "ตราด", "ocean", "https://www.agoda.com/kc-grande-resort-spa/hotel/koh-chang-th.html", 2700),
        new("th-stay-072", "Centara Ubon", "อุบลราชธานี", "nan", "https://www.agoda.com/en-za/centara-ubon/hotel/ubon-ratchathani-th.html", 2950),
        new("th-stay-073", "Centara Udon", "อุดรธานี", "urban", "https://www.agoda.com/centara-hotel-convention-centre-udon-thani/hotel/udon-thani-th.html", 1200),
        new("th-stay-074", "Uthai Heritage Hotel", "อุทัยธานี", "mountain", "https://www.agoda.com/mueang-uthai-thani/maps/uthai-thani-th.html", 1450),
        new("th-stay-075", "Seeharaj Hotel", "อุตรดิตถ์", "river", "https://www.agoda.com/en-gb/region/uttaradit-province-th.html", 1700),
        new("th-stay-076", "Grand Mandarin Betong Hotel", "ยะลา", "garden", "https://www.agoda.com/mandarin-betong/hotel/betong-th.html", 1950),
        new("th-stay-077", "JP Emerald Hotel", "ยโสธร", "forest", "https://www.agoda.com/jp-emerald-hotel/hotel/yasothon-th.html", 2200),
        new("th-stay-078", "Ramiarndao Boutique Place Hotel", "อำนาจเจริญ", "nan", "https://www.agoda.com/en-au/city/amnat-charoen-th.html", 2450),
        new("th-stay-079", "IKKYU2 Angthong", "อ่างทอง", "urban", "https://www.agoda.com/theme/free-cancellation/city/ang-thong-th.html", 2700),
        new("th-stay-080", "B2 Bueng Kan Boutique & Budget Hotel", "บึงกาฬ", "mountain", "https://www.agoda.com/id-id/region/bueng-kan-province-th.html", 2950),
        new("th-stay-081", "Modena by Fraser Buriram", "บุรีรัมย์", "river", "https://www.agoda.com/cs-cz/theme/resorts/city/buriram-th.html", 1200),
        new("th-stay-082", "Top Inn Hotel", "มหาสารคาม", "garden", "https://www.agoda.com/region/maha-sarakham-province-th.html", 1450),
        new("th-stay-083", "A BIZZ Hotel", "ลำปาง", "forest", "https://www.agoda.com/en-in/hotels-near-home-at-lampang/attractions/lampang-th.html", 1700),
        new("th-stay-084", "O2 Hotel Lopburi", "ลพบุรี", "nan", "https://www.agoda.com/region/lop-buri-province-th.html", 1950),
        new("th-stay-085", "Indiego Space", "เลย", "urban", "https://www.agoda.com/en-ie/city/loei-th.html", 2200),
        new("th-stay-086", "Reverie Siam Resort", "แม่ฮ่องสอน", "mountain", "https://www.agoda.com/en-sg/region/mae-hong-son-province-th.html", 2450),
        new("th-stay-087", "Dee Prom Hotel", "ชัยภูมิ", "river", "https://www.agoda.com/city/chaiyaphum-th.html", 2700),
        new("th-stay-088", "Rimpao Hotel", "กาฬสินธุ์", "garden", "https://www.agoda.com/city/kalasin-th.html", 2950),
        new("th-stay-089", "Greenpark Hotel", "กำแพงเพชร", "forest", "https://www.agoda.com/region/kamphaeng-phet-province-th.html", 1200),
        new("th-stay-090", "Maldives Beach Resort", "จันทบุรี", "nan", "https://www.agoda.com/theme/agoda-home/city/chanthaburi-th.html", 1450),
        new("th-stay-091", "Hi Chiangrai Hotel", "เชียงราย", "urban", "https://www.agoda.com/destination/city/chiang-rai-th.html", 1700),
        new("th-stay-092", "Grande Centre Point Voyage Pattaya", "ชลบุรี", "sunset", "https://www.agoda.com/grande-centre-point-voyage-pattaya/hotel/pattaya-th.html", 1950),
        new("th-stay-093", "Twin Lotus Hotel", "นครศรีธรรมราช", "river", "https://www.agoda.com/city/nakhon-si-thammarat-th.html", 2200),
        new("th-stay-094", "Centara Korat", "นครราชสีมา", "garden", "https://www.agoda.com/en-us/hotels-near-the-mall-ratchasima/attractions/nakhonratchasima-th.html", 2450),
        new("th-stay-095", "Siva Royal Hotel", "พัทลุง", "forest", "https://www.agoda.com/city/phatthalung-th.html", 2700),
        new("th-stay-096", "M2 Hotel", "พะเยา", "nan", "https://www.agoda.com/region/phayao-province-th.html", 2950),
        new("th-stay-097", "Livist Resort phetchabun", "เพชรบูรณ์", "urban", "https://www.agoda.com/region/phetchabun-province-th.html", 1200),
        new("th-stay-098", "Fahluang Residence", "พิจิตร", "mountain", "https://www.agoda.com/en-gb/region/phichit-province-th.html", 1450),
        new("th-stay-099", "Hotel de Ladda", "มุกดาหาร", "river", "https://www.agoda.com/city/mukdahan-th.html", 1700),
        new("th-stay-100", "Sida Resort Hotel Nakhon Nayok", "นครนายก", "garden", "https://www.agoda.com/en-in/theme/agoda-home/city/nakhon-nayok-th.html", 1950),
    ]);

    // Existing orders keep their original snapshot and illustrative image. Never reuse old IDs.
    private static readonly HashSet<string> LegacyImages = new(StringComparer.Ordinal)
    { "forest", "ocean", "urban", "garden", "sunset", "river", "mountain", "nan", "huahin", "pattaya", "samui", "kanchanaburi" };
    public static string ImageFor(string hotelId) => All.FirstOrDefault(h => h.Id == hotelId)?.ImageKey
        ?? (LegacyImages.Contains(hotelId) ? hotelId : "urban");
}
