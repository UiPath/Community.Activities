using UiPath.Cryptography.Properties;

namespace UiPath.Cryptography.Enums
{
    /// <summary>
    /// code pages
    /// taken from System package
    /// </summary>
    public enum CodePages
    {
        [LocalizedDisplayName(nameof(Resources.DefaultCodePage))]
        Default = 0,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_US_Canada))]
        IBM037 = 37,
        [LocalizedDisplayName(nameof(Resources.OEMUnitedStates))]
        IBM437 = 437,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_International))]
        IBM500 = 500,
        [LocalizedDisplayName(nameof(Resources.Arabic_ASMO708))]
        ASMO_708 = 708,
        [LocalizedDisplayName(nameof(Resources.Arabic_DOS))]
        DOS_720 = 720,
        [LocalizedDisplayName(nameof(Resources.Greek_DOS))]
        IBM737 = 737,
        [LocalizedDisplayName(nameof(Resources.Baltic_DOS))]
        IBM775 = 775,
        [LocalizedDisplayName(nameof(Resources.WesternEuropean_DOS))]
        IBM850 = 850,
        [LocalizedDisplayName(nameof(Resources.CentralEuropean_DOS))]
        IBM852 = 852,
        [LocalizedDisplayName(nameof(Resources.OEMCyrillic))]
        IBM855 = 855,
        [LocalizedDisplayName(nameof(Resources.Turkish_DOS))]
        IBM857 = 857,
        [LocalizedDisplayName(nameof(Resources.OEMMultilingualLatinI))]
        IBM00858 = 858,
        [LocalizedDisplayName(nameof(Resources.Portuguese_DOS))]
        IBM860 = 860,
        [LocalizedDisplayName(nameof(Resources.Icelandic_DOS))]
        IBM861 = 861,
        [LocalizedDisplayName(nameof(Resources.Hebrew_DOS))]
        DOS_862 = 862,
        [LocalizedDisplayName(nameof(Resources.FrenchCanadian_DOS))]
        IBM863 = 863,
        [LocalizedDisplayName(nameof(Resources.Arabic_864))]
        IBM864 = 864,
        [LocalizedDisplayName(nameof(Resources.Nordic_DOS))]
        IBM865 = 865,
        [LocalizedDisplayName(nameof(Resources.Cyrillic_DOS))]
        CP866 = 866,
        [LocalizedDisplayName(nameof(Resources.Greek_Modern_DOS))]
        IBM869 = 869,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_MultilingualLatin_2))]
        IBM870 = 870,
        [LocalizedDisplayName(nameof(Resources.Thai_Windows))]
        WINDOWS_874 = 874,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_GreekModern))]
        CP875 = 875,
        [LocalizedDisplayName(nameof(Resources.Japanese_Shift_JIS))]
        SHIFT_JIS = 932,
        [LocalizedDisplayName(nameof(Resources.ChineseSimplified_GB2312))]
        GB2312 = 936,
        [LocalizedDisplayName(nameof(Resources.Korean))]
        KS_C_5601_1987 = 949,
        [LocalizedDisplayName(nameof(Resources.ChineseTraditional_Big5))]
        BIG5 = 950,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_TurkishLatin_5))]
        IBM1026 = 1026,
        [LocalizedDisplayName(nameof(Resources.IBMLatin_1))]
        IBM01047 = 1047,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_US_Canada_Euro))]
        IBM01140 = 1140,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_Germany_Euro))]
        IBM01141 = 1141,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_Denmark_Norway_Euro))]
        IBM01142 = 1142,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_Finland_Sweden_Euro))]
        IBM01143 = 1143,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_Italy_Euro))]
        IBM01144 = 1144,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_Spain_Euro))]
        IBM01145 = 1145,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_UK_Euro))]
        IBM01146 = 1146,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_France_Euro))]
        IBM01147 = 1147,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_International_Euro))]
        IBM01148 = 1148,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_Icelandic_Euro))]
        IBM01149 = 1149,
        [LocalizedDisplayName(nameof(Resources.Unicode))]
        UTF_16 = 1200,
        [LocalizedDisplayName(nameof(Resources.Unicode_Big_Endian))]
        UTF_16BE = 1201,
        [LocalizedDisplayName(nameof(Resources.CentralEuropean_Windows))]
        WINDOWS_1250 = 1250,
        [LocalizedDisplayName(nameof(Resources.Cyrillic_Windows))]
        WINDOWS_1251 = 1251,
        [LocalizedDisplayName(nameof(Resources.WesternEuropean_Windows))]
        WINDOWS_1252 = 1252,
        [LocalizedDisplayName(nameof(Resources.Greek_Windows))]
        WINDOWS_1253 = 1253,
        [LocalizedDisplayName(nameof(Resources.Turkish_Windows))]
        WINDOWS_1254 = 1254,
        [LocalizedDisplayName(nameof(Resources.Hebrew_Windows))]
        WINDOWS_1255 = 1255,
        [LocalizedDisplayName(nameof(Resources.Arabic_Windows))]
        WINDOWS_1256 = 1256,
        [LocalizedDisplayName(nameof(Resources.Baltic_Windows))]
        WINDOWS_1257 = 1257,
        [LocalizedDisplayName(nameof(Resources.Vietnamese_Windows))]
        WINDOWS_1258 = 1258,
        [LocalizedDisplayName(nameof(Resources.Korean_Johab))]
        JOHAB = 1361,
        [LocalizedDisplayName(nameof(Resources.WesternEuropean_Mac))]
        MACINTOSH = 10000,
        [LocalizedDisplayName(nameof(Resources.Japanese_Mac))]
        X_MAC_JAPANESE = 10001,
        [LocalizedDisplayName(nameof(Resources.ChineseTraditional_Mac))]
        X_MAC_CHINESETRAD = 10002,
        [LocalizedDisplayName(nameof(Resources.Korean_Mac))]
        X_MAC_KOREAN = 10003,
        [LocalizedDisplayName(nameof(Resources.Arabic_Mac))]
        X_MAC_ARABIC = 10004,
        [LocalizedDisplayName(nameof(Resources.Hebrew_Mac))]
        X_MAC_HEBREW = 10005,
        [LocalizedDisplayName(nameof(Resources.Greek_Mac))]
        X_MAC_GREEK = 10006,
        [LocalizedDisplayName(nameof(Resources.Cyrillic_Mac))]
        X_MAC_CYRILLIC = 10007,
        [LocalizedDisplayName(nameof(Resources.ChineseSimplified_Mac))]
        X_MAC_CHINESESIMP = 10008,
        [LocalizedDisplayName(nameof(Resources.Romanian_Mac))]
        X_MAC_ROMANIAN = 10010,
        [LocalizedDisplayName(nameof(Resources.Ukrainian_Mac))]
        X_MAC_UKRAINIAN = 10017,
        [LocalizedDisplayName(nameof(Resources.Thai_Mac))]
        X_MAC_THAI = 10021,
        [LocalizedDisplayName(nameof(Resources.CentralEuropean_Mac))]
        X_MAC_CE = 10029,
        [LocalizedDisplayName(nameof(Resources.Icelandic_Mac))]
        X_MAC_ICELANDIC = 10079,
        [LocalizedDisplayName(nameof(Resources.Turkish_Mac))]
        X_MAC_TURKISH = 10081,
        [LocalizedDisplayName(nameof(Resources.Croatian_Mac))]
        X_MAC_CROATIAN = 10082,
        [LocalizedDisplayName(nameof(Resources.Unicode_UTF_32))]
        UTF_32 = 12000,
        [LocalizedDisplayName(nameof(Resources.Unicode_UTF_32Big_Endian))]
        UTF_32BE = 12001,
        [LocalizedDisplayName(nameof(Resources.ChineseTraditional_CNS))]
        X_CHINESE_CNS = 20000,
        [LocalizedDisplayName(nameof(Resources.TCATaiwan))]
        X_CP20001 = 20001,
        [LocalizedDisplayName(nameof(Resources.ChineseTraditional_Eten))]
        X_CHINESE_ETEN = 20002,
        [LocalizedDisplayName(nameof(Resources.IBM5550Taiwan))]
        X_CP20003 = 20003,
        [LocalizedDisplayName(nameof(Resources.TeleTextTaiwan))]
        X_CP20004 = 20004,
        [LocalizedDisplayName(nameof(Resources.WangTaiwan))]
        X_CP20005 = 20005,
        [LocalizedDisplayName(nameof(Resources.WesternEuropean_IA5))]
        X_IA5 = 20105,
        [LocalizedDisplayName(nameof(Resources.German_IA5))]
        X_IA5_GERMAN = 20106,
        [LocalizedDisplayName(nameof(Resources.Swedish_IA5))]
        X_IA5_SWEDISH = 20107,
        [LocalizedDisplayName(nameof(Resources.Norwegian_IA5))]
        X_IA5_NORWEGIAN = 20108,
        [LocalizedDisplayName(nameof(Resources.US_ASCII))]
        US_ASCII = 20127,
        [LocalizedDisplayName(nameof(Resources.T61))]
        X_CP20261 = 20261,
        [LocalizedDisplayName(nameof(Resources.ISO_6937))]
        X_CP20269 = 20269,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_Germany))]
        IBM273 = 20273,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_Denmark_Norway))]
        IBM277 = 20277,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_Finland_Sweden))]
        IBM278 = 20278,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_Italy))]
        IBM280 = 20280,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_Spain))]
        IBM284 = 20284,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_UK))]
        IBM285 = 20285,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_Japanesekatakana))]
        IBM290 = 20290,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_France))]
        IBM297 = 20297,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_Arabic))]
        IBM420 = 20420,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_Greek))]
        IBM423 = 20423,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_Hebrew))]
        IBM424 = 20424,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_KoreanExtended))]
        X_EBCDIC_KOREANEXTENDED = 20833,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_Thai))]
        IBM_THAI = 20838,
        [LocalizedDisplayName(nameof(Resources.Cyrillic_KOI8_R))]
        KOI8_R = 20866,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_Icelandic))]
        IBM871 = 20871,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_CyrillicRussian))]
        IBM880 = 20880,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_Turkish))]
        IBM905 = 20905,
        [LocalizedDisplayName(nameof(Resources.IBMLatin_1))]
        IBM00924 = 20924,
        [LocalizedDisplayName(nameof(Resources.Japanese_JIS0208_1990and0212_1990))]
        Japanese_JIS0208_1990and0212_1990 = 20932,
        [LocalizedDisplayName(nameof(Resources.ChineseSimplified_GB2312_80))]
        X_CP20936 = 20936,
        [LocalizedDisplayName(nameof(Resources.KoreanWansung))]
        X_CP20949 = 20949,
        [LocalizedDisplayName(nameof(Resources.IBMEBCDIC_CyrillicSerbian_Bulgarian))]
        CP1025 = 21025,
        [LocalizedDisplayName(nameof(Resources.Cyrillic_KOI8_U))]
        KOI8_U = 21866,
        [LocalizedDisplayName(nameof(Resources.WesternEuropean_ISO))]
        ISO_8859_1 = 28591,
        [LocalizedDisplayName(nameof(Resources.CentralEuropean_ISO))]
        ISO_8859_2 = 28592,
        [LocalizedDisplayName(nameof(Resources.Latin3_ISO))]
        ISO_8859_3 = 28593,
        [LocalizedDisplayName(nameof(Resources.Baltic_ISO))]
        ISO_8859_4 = 28594,
        [LocalizedDisplayName(nameof(Resources.Cyrillic_ISO))]
        ISO_8859_5 = 28595,
        [LocalizedDisplayName(nameof(Resources.Arabic_ISO))]
        ISO_8859_6 = 28596,
        [LocalizedDisplayName(nameof(Resources.Greek_ISO))]
        ISO_8859_7 = 28597,
        [LocalizedDisplayName(nameof(Resources.Hebrew_ISO_Visual))]
        ISO_8859_8 = 28598,
        [LocalizedDisplayName(nameof(Resources.Turkish_ISO))]
        ISO_8859_9 = 28599,
        [LocalizedDisplayName(nameof(Resources.Estonian_ISO))]
        ISO_8859_13 = 28603,
        [LocalizedDisplayName(nameof(Resources.Latin9_ISO))]
        ISO_8859_15 = 28605,
        [LocalizedDisplayName(nameof(Resources.Europa))]
        X_EUROPA = 29001,
        [LocalizedDisplayName(nameof(Resources.Hebrew_ISO_Logical))]
        ISO_8859_8_I = 38598,
        [LocalizedDisplayName(nameof(Resources.Japanese_JIS))]
        ISO_2022_JP = 50220,
        [LocalizedDisplayName(nameof(Resources.Japanese_JIS_Allow1byteKana))]
        CSISO2022JP = 50221,
        [LocalizedDisplayName(nameof(Resources.Japanese_JIS_Allow1byteKana_SO_SI))]
        Japanese_JIS_Allow1byteKana_SO_SI = 50222,
        [LocalizedDisplayName(nameof(Resources.Korean_ISO))]
        ISO_2022_KR = 50225,
        [LocalizedDisplayName(nameof(Resources.ChineseSimplified_ISO_2022))]
        X_CP50227 = 50227,
        [LocalizedDisplayName(nameof(Resources.Japanese_EUC))]
        EUC_JP = 51932,
        [LocalizedDisplayName(nameof(Resources.ChineseSimplified_EUC))]
        EUC_CN = 51936,
        [LocalizedDisplayName(nameof(Resources.Korean_EUC))]
        EUC_KR = 51949,
        [LocalizedDisplayName(nameof(Resources.ChineseSimplified_HZ))]
        HZ_GB_2312 = 52936,
        [LocalizedDisplayName(nameof(Resources.ChineseSimplified_GB18030))]
        GB18030 = 54936,
        [LocalizedDisplayName(nameof(Resources.ISCIIDevanagari))]
        X_ISCII_DE = 57002,
        [LocalizedDisplayName(nameof(Resources.ISCIIBengali))]
        X_ISCII_BE = 57003,
        [LocalizedDisplayName(nameof(Resources.ISCIITamil))]
        X_ISCII_TA = 57004,
        [LocalizedDisplayName(nameof(Resources.ISCIITelugu))]
        X_ISCII_TE = 57005,
        [LocalizedDisplayName(nameof(Resources.ISCIIAssamese))]
        X_ISCII_AS = 57006,
        [LocalizedDisplayName(nameof(Resources.ISCIIOriya))]
        X_ISCII_OR = 57007,
        [LocalizedDisplayName(nameof(Resources.ISCIIKannada))]
        X_ISCII_KA = 57008,
        [LocalizedDisplayName(nameof(Resources.ISCIIMalayalam))]
        X_ISCII_MA = 57009,
        [LocalizedDisplayName(nameof(Resources.ISCIIGujarati))]
        X_ISCII_GU = 57010,
        [LocalizedDisplayName(nameof(Resources.ISCIIPunjabi))]
        X_ISCII_PA = 57011,
        [LocalizedDisplayName(nameof(Resources.Unicode_UTF_7))]
        UTF_7 = 65000,
        [LocalizedDisplayName(nameof(Resources.Unicode_UTF_8))]
        UTF_8 = 65001
    }
}
