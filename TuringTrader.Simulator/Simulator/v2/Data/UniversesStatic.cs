//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        StaticUniverses
// Description: Static universe definitions.
// History:     2022xii01, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ trading simulator.
//              TuringTrader is free software: you can redistribute it and/or 
//              modify it under the terms of the GNU Affero General Public 
//              License as published by the Free Software Foundation, either 
//              version 3 of the License, or (at your option) any later version.
//              TuringTrader is distributed in the hope that it will be useful,
//              but WITHOUT ANY WARRANTY; without even the implied warranty of
//              MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//              GNU Affero General Public License for more details.
//              You should have received a copy of the GNU Affero General Public
//              License along with TuringTrader. If not, see 
//              https://www.gnu.org/licenses/agpl-3.0.
//==============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace TuringTrader.SimulatorV2
{
    public static partial class DataSource
    {
        #region static spx
        private static List<string> _staticSpx = new List<string>
        {
            // as of 12/01/2022, see https://www.slickcharts.com/sp500
            "AAPL",  // 1   Apple Inc. AAPL
            "MSFT",  // 2   Microsoft Corporation MSFT
            "AMZN",  // 3   Amazon.com Inc. AMZN
            "GOOGL", // 4   Alphabet Inc. Class A   GOOGL
            "BRK.B", // 5   Berkshire Hathaway Inc. Class B BRK.B
            "GOOG",  // 6   Alphabet Inc. Class C   GOOG
            "TSLA",  // 7   Tesla Inc   TSLA
            "UNH",   // 8   UnitedHealth Group Incorporated UNH
            "JNJ",   // 9   Johnson & Johnson   JNJ
            "XOM",   // 10  Exxon Mobil Corporation XOM
            "NVDA",  // 11  NVIDIA Corporation  NVDA
            "JPM",   // 12  JPMorgan Chase & Co.    JPM
            "PG",    // 13  Procter & Gamble Company    PG
            "V",     // 14  Visa Inc. Class A   V
            "HD",    // 15  Home Depot Inc. HD
            "CVX",   // 16  Chevron Corporation CVX
            "MA",    // 17  Mastercard Incorporated Class A MA
            "LLY",   // 18  Eli Lilly and Company   LLY
            "ABBV",  // 19  AbbVie Inc. ABBV
            "PFE",   // 20  Pfizer Inc. PFE
            "MRK",   // 21  Merck & Co. Inc.    MRK
            "META",  // 22  Meta Platforms Inc. Class A META
            "BAC",   // 23  Bank of America Corp    BAC
            "PEP",   // 24  PepsiCo Inc.    PEP
            "KO",    // 25  Coca-Cola Company   KO
            "COST",  // 26  Costco Wholesale Corporation    COST
            "AVGO",  // 27  Broadcom Inc.   AVGO
            "TMO",   // 28  Thermo Fisher Scientific Inc.   TMO
            "WMT",   // 29  Walmart Inc.    WMT
            "CSCO",  // 30  Cisco Systems Inc.  CSCO
            "MCD",   // 31  McDonald's Corporation	MCD
            "ACN",   // 32  Accenture Plc Class A   ACN
            "ABT",   // 33  Abbott Laboratories ABT
            "WFC",   // 34  Wells Fargo & Company   WFC
            "DHR",   // 35  Danaher Corporation DHR
            "DIS",   // 36  Walt Disney Company DIS
            "BMY",   // 37  Bristol-Myers Squibb Company    BMY
            "LIN",   // 38  Linde plc   LIN
            "NEE",   // 39  NextEra Energy Inc. NEE
            "TXN",   // 40  Texas Instruments Incorporated  TXN
            "VZ",    // 41  Verizon Communications Inc. VZ
            "ADBE",  // 42  Adobe Incorporated  ADBE
            "CMCSA", // 43  Comcast Corporation Class A CMCSA
            "CRM",   // 44  Salesforce Inc. CRM
            "COP",   // 45  ConocoPhillips  COP
            "PM",    // 46  Philip Morris International Inc.    PM
            "AMGN",  // 47  Amgen Inc.  AMGN
            "HON",   // 48  Honeywell International Inc.    HON
            "RTX",   // 49  Raytheon Technologies Corporation   RTX
            "QCOM",  // 50  QUALCOMM Incorporated   QCOM
            "UPS",   // 51  United Parcel Service Inc. Class B  UPS
            "NKE",   // 52  NIKE Inc. Class B   NKE
            "T",     // 53  AT&T Inc.   T
            "NFLX",  // 54  Netflix Inc.    NFLX
            "LOW",   // 55  Lowe's Companies Inc.	LOW
            "UNP",   // 56  Union Pacific Corporation   UNP
            "IBM",   // 57  International Business Machines Corporation IBM
            "CVS",   // 58  CVS Health Corporation  CVS
            "GS",    // 59  Goldman Sachs Group Inc.    GS
            "ELV",   // 60  Elevance Health Inc.    ELV
            "ORCL",  // 61  Oracle Corporation  ORCL
            "SCHW",  // 62  Charles Schwab Corp SCHW
            "AMD",   // 63  Advanced Micro Devices Inc. AMD
            "CAT",   // 64  Caterpillar Inc.    CAT
            "MS",    // 65  Morgan Stanley  MS
            "INTC",  // 66  Intel Corporation   INTC
            "DE",    // 67  Deere & Company DE
            "SPGI",  // 68  S&P Global Inc. SPGI
            "SBUX",  // 69  Starbucks Corporation   SBUX
            "INTU",  // 70  Intuit Inc. INTU
            "LMT",   // 71  Lockheed Martin Corporation LMT
            "GILD",  // 72  Gilead Sciences Inc.    GILD
            "ADP",   // 73  Automatic Data Processing Inc.  ADP
            "PLD",   // 74  Prologis Inc.   PLD
            "BLK",   // 75  BlackRock Inc.  BLK
            "MDT",   // 76  Medtronic Plc   MDT
            "AMT",   // 77  American Tower Corporation  AMT
            "CI",    // 78  Cigna Corporation   CI
            "BA",    // 79  Boeing Company  BA
            "ISRG",  // 80  Intuitive Surgical Inc. ISRG
            "AMAT",  // 81  Applied Materials Inc.  AMAT
            "AXP",   // 82  American Express Company    AXP
            "GE",    // 83  General Electric Company    GE
            "TJX",   // 84  TJX Companies Inc   TJX
            "C",     // 85  Citigroup Inc.  C
            "MDLZ",  // 86  Mondelez International Inc. Class A MDLZ
            "CB",    // 87  Chubb Limited   CB
            "TMUS",  // 88  T-Mobile US Inc.    TMUS
            "PYPL",  // 89  PayPal Holdings Inc.    PYPL
            "ADI",   // 90  Analog Devices Inc. ADI
            "MMC",   // 91  Marsh & McLennan Companies Inc. MMC
            "NOW",   // 92  ServiceNow Inc. NOW
            "MO",    // 93  Altria Group Inc    MO
            "EOG",   // 94  EOG Resources Inc.  EOG
            "BKNG",  // 95  Booking Holdings Inc.   BKNG
            "VRTX",  // 96  Vertex Pharmaceuticals Incorporated VRTX
            "REGN",  // 97  Regeneron Pharmaceuticals Inc.  REGN
            "SYK",   // 98  Stryker Corporation SYK
            "NOC",   // 99  Northrop Grumman Corp.  NOC
            "TGT",   // 100 Target Corporation  TGT
            "PGR",   // 101 Progressive Corporation PGR
            "DUK",   // 102 Duke Energy Corporation DUK
            "SLB",   // 103 Schlumberger NV SLB
            "ZTS",   // 104 Zoetis Inc. Class A ZTS
            "SO",    // 105 Southern Company    SO
            "BDX",   // 106 Becton Dickinson and Company    BDX
            "CSX",   // 107 CSX Corporation CSX
            "MMM",   // 108 3M Company  MMM
            "HUM",   // 109 Humana Inc. HUM
            "PNC",   // 110 PNC Financial Services Group Inc.   PNC
            "ADP",   // 111 Air Products and Chemicals Inc. APD
            "FISV",  // 112 Fiserv Inc. FISV
            "ETN",   // 113 Eaton Corp. Plc ETN
            "AON",   // 114 Aon Plc Class A AON
            "CL",    // 115 Colgate-Palmolive Company   CL
            "LRCX",  // 116 Lam Research Corporation    LRCX
            "BSX",   // 117 Boston Scientific Corporation   BSX
            "ITW",   // 118 Illinois Tool Works Inc.    ITW
            "MU",    // 119 Micron Technology Inc.  MU
            "WM",    // 120 Waste Management Inc.   WM
            "CME",   // 121 CME Group Inc. Class A  CME
            "EQIX",  // 122 Equinix Inc.    EQIX
            "TFC",   // 123 Truist Financial Corporation    TFC
            "USB",   // 124 U.S. Bancorp    USB
            "CCI",   // 125 Crown Castle Inc.   CCI
            "MPC",   // 126 Marathon Petroleum Corporation  MPC
            "ICE",   // 127 Intercontinental Exchange Inc.  ICE
            "NSC",   // 128 Norfolk Southern Corporation    NSC
            "MRNA",  // 129 Moderna Inc.    MRNA
            "GM",    // 130 General Motors Company  GM
            "SHW",   // 131 Sherwin-Williams Company    SHW
            "DG",    // 132 Dollar General Corporation  DG
            "FCX",   // 133 Freeport-McMoRan Inc.   FCX
            "GD",    // 134 General Dynamics Corporation    GD
            "EMR",   // 135 Emerson Electric Co.    EMR
            "PXD",   // 136 Pioneer Natural Resources Company   PXD
            "KLAC",  // 137 KLA Corporation KLAC
            "ORLY",  // 138 O'Reilly Automotive Inc.	ORLY
            "F",     // 139 Ford Motor Company  F
            "MCK",   // 140 McKesson Corporation    MCK
            "ADM",   // 141 Archer-Daniels-Midland Company  ADM
            "EL",    // 142 Estee Lauder Companies Inc. Class A EL
            "ATVI",  // 143 Activision Blizzard Inc.    ATVI
            "VLO",   // 144 Valero Energy Corporation   VLO
            "SRE",   // 145 Sempra Energy   SRE
            "PSX",   // 146 Phillips 66 PSX
            "SNPS",  // 147 Synopsys Inc.   SNPS
            "OXY",   // 148 Occidental Petroleum Corporation    OXY
            "HCA",   // 149 HCA Healthcare Inc  HCA
            "MET",   // 150 MetLife Inc.    MET
            "D",     // 151 Dominion Energy Inc D
            "GIS",   // 152 General Mills Inc.  GIS
            "AZO",   // 153 AutoZone Inc.   AZO
            "CNC",   // 154 Centene Corporation CNC
            "AEP",   // 155 American Electric Power Company Inc.    AEP
            "CTVA",  // 156 Corteva Inc CTVA
            "AIG",   // 157 American International Group Inc.   AIG
            "EW",    // 158 Edwards Lifesciences Corporation    EW
            "APH",   // 159 Amphenol Corporation Class A    APH
            "CDNS",  // 160 Cadence Design Systems Inc. CDNS
            "PSA",   // 161 Public Storage  PSA
            "MCO",   // 162 Moody's Corporation	MCO
            "ROP",   // 163 Roper Technologies Inc. ROP
            "A",     // 164 Agilent Technologies Inc.   A
            "NXPI",  // 165 NXP Semiconductors NV   NXPI
            "KMB",   // 166 Kimberly-Clark Corporation  KMB
            "JCI",   // 167 Johnson Controls International plc  JCI
            "DXCM",  // 168 DexCom Inc. DXCM
            "MSI",   // 169 Motorola Solutions Inc. MSI
            "MAR",   // 170 Marriott International Inc. Class A MAR
            "CMG",   // 171 Chipotle Mexican Grill Inc. CMG
            "TRV",   // 172 Travelers Companies Inc.    TRV
            "DVN",   // 173 Devon Energy Corporation    DVN
            "BIIB",  // 174 Biogen Inc. BIIB
            "FIS",   // 175 Fidelity National Information Services Inc. FIS
            "SYY",   // 176 Sysco Corporation   SYY
            "ADSK",  // 177 Autodesk Inc.   ADSK
            "MCHP",  // 178 Microchip Technology Incorporated   MCHP
            "ENPH",  // 179 Enphase Energy Inc. ENPH
            "LHX",   // 180 L3Harris Technologies Inc   LHX
            "CHTR",  // 181 Charter Communications Inc. Class A CHTR
            "FDX",   // 182 FedEx Corporation   FDX
            "WMB",   // 183 Williams Companies Inc. WMB
            "AJG",   // 184 Arthur J. Gallagher & Co.   AJG
            "TT",    // 185 Trane Technologies plc  TT
            "AFL",   // 186 Aflac Incorporated  AFL
            "ROST",  // 187 Ross Stores Inc.    ROST
            "EXC",   // 188 Exelon Corporation  EXC
            "MSCI",  // 189 MSCI Inc. Class A   MSCI
            "STZ",   // 190 Constellation Brands Inc. Class A   STZ
            "IQV",   // 191 IQVIA Holdings Inc  IQV
            "TEL",   // 192 TE Connectivity Ltd.    TEL
            "PRU",   // 193 Prudential Financial Inc.   PRU
            "HES",   // 194 Hess Corporation    HES
            "CTAS",  // 195 Cintas Corporation  CTAS
            "COF",   // 196 Capital One Financial Corp  COF
            "MNST",  // 197 Monster Beverage Corporation    MNST
            "PAYX",  // 198 Paychex Inc.    PAYX
            "NUE",   // 199 Nucor Corporation   NUE
            "HLT",   // 200 Hilton Worldwide Holdings Inc   HLT
            "O",     // 201 Realty Income Corporation   O
            "SPG",   // 202 Simon Property Group Inc.   SPG
            "PH",    // 203 Parker-Hannifin Corporation PH
            "XEL",   // 204 Xcel Energy Inc.    XEL
            "KMI",   // 205 Kinder Morgan Inc Class P   KMI
            "NEM",   // 206 Newmont Corporation NEM
            "CARR",  // 207 Carrier Global Corp.    CARR
            "ECL",   // 208 Ecolab Inc. ECL
            "DOW",   // 209 Dow Inc.    DOW
            "YUM",   // 210 Yum! Brands Inc.    YUM
            "PCAR",  // 211 PACCAR Inc  PCAR
            "ALL",   // 212 Allstate Corporation    ALL
            "AMP",   // 213 Ameriprise Financial Inc.   AMP
            "IDXX",  // 214 IDEXX Laboratories Inc. IDXX
            "CMI",   // 215 Cummins Inc.    CMI
            "DD",    // 216 DuPont de Nemours Inc.  DD
            "FTNT",  // 217 Fortinet Inc.   FTNT
            "EA",    // 218 Electronic Arts Inc.    EA
            "HSY",   // 219 Hershey Company HSY
            "ED",    // 220 Consolidated Edison Inc.    ED
            "ANET",  // 221 Arista Networks Inc.    ANET
            "HAL",   // 222 Halliburton Company HAL
            "ILMN",  // 223 Illumina Inc.   ILMN
            "BK",    // 224 Bank of New York Mellon Corp    BK
            "RMD",   // 225 ResMed Inc. RMD
            "MTD",   // 226 Mettler-Toledo International Inc.   MTD
            "WELL",  // 227 Welltower Inc   WELL
            "KDP",   // 228 Keurig Dr Pepper Inc.   KDP
            "VICI",  // 229 VICI Properties Inc VICI
            "OTIS",  // 230 Otis Worldwide Corporation  OTIS
            "AME",   // 231 AMETEK Inc. AME
            "ON",    // 232 ON Semiconductor Corporation    ON
            "KEYS",  // 233 Keysight Technologies Inc   KEYS
            "TDG",   // 234 TransDigm Group Incorporated    TDG
            "DLR",   // 235 Digital Realty Trust Inc.   DLR
            "SBAC",  // 236 SBA Communications Corp. Class A    SBAC
            "ALB",   // 237 Albemarle Corporation   ALB
            "CTSH",  // 238 Cognizant Technology Solutions Corporation Class A  CTSH
            "KR",    // 239 Kroger Co.  KR
            "CSGP",  // 240 CoStar Group Inc.   CSGP
            "PPG",   // 241 PPG Industries Inc. PPG
            "DLTR",  // 242 Dollar Tree Inc.    DLTR
            "KHC",   // 243 Kraft Heinz Company KHC
            "CEG",   // 244 Constellation Energy Corporation    CEG
            "WEC",   // 245 WEC Energy Group Inc    WEC
            "ROK",   // 246 Rockwell Automation Inc.    ROK
            "PEG",   // 247 Public Service Enterprise Group Inc PEG
            "MTB",   // 248 M&T Bank Corporation    MTB
            "DFS",   // 249 Discover Financial Services DFS
            "OKE",   // 250 ONEOK Inc.  OKE
            "WBA",   // 251 Walgreens Boots Alliance Inc.   WBA
            "BKR",   // 252 Baker Hughes Company Class A    BKR
            "FAST",  // 253 Fastenal Company    FAST
            "STT",   // 254 State Street Corporation    STT
            "VRSK",  // 255 Verisk Analytics Inc    VRSK
            "GPN",   // 256 Global Payments Inc.    GPN
            "ES",    // 257 Eversource Energy   ES
            "RSG",   // 258 Republic Services Inc.  RSG
            "APTV",  // 259 Aptiv PLC   APTV
            "BAX",   // 260 Baxter International Inc.   BAX
            "CPRT",  // 261 Copart Inc. CPRT
            "TROW",  // 262 T. Rowe Price Group TROW
            "ODFL",  // 263 Old Dominion Freight Line Inc.  ODFL
            "IT",    // 264 Gartner Inc.    IT
            "HPQ",   // 265 HP Inc. HPQ
            "GWW",   // 266 W.W. Grainger Inc.  GWW
            "AWK",   // 267 American Water Works Company Inc.   AWK
            "DHI",   // 268 D.R. Horton Inc.    DHI
            "WTW",   // 269 Willis Towers Watson Public Limited Company WTW
            "IFF",   // 270 International Flavors & Fragrances Inc. IFF
            "ABC",   // 271 AmerisourceBergen Corporation   ABC
            "FANG",  // 272 Diamondback Energy Inc. FANG
            "GPC",   // 273 Genuine Parts Company   GPC
            "GLW",   // 274 Corning Inc GLW
            "CBRE",  // 275 CBRE Group Inc. Class A CBRE
            "CDW",   // 276 CDW Corp.   CDW
            "TSCO",  // 277 Tractor Supply Company  TSCO
            "EIX",   // 278 Edison International    EIX
            "WBD",   // 279 Warner Bros. Discovery Inc. Series A    WBD
            "EBAY",  // 280 eBay Inc.   EBAY
            "PCG",   // 281 PG&E Corporation    PCG
            "ZBH",   // 282 Zimmer Biomet Holdings Inc. ZBH
            "URI",   // 283 United Rentals Inc. URI
            "HIG",   // 284 Hartford Financial Services Group Inc.  HIG
            "FITB",  // 285 Fifth Third Bancorp FITB
            "WY",    // 286 Weyerhaeuser Company    WY
            "ULTA",  // 287 Ulta Beauty Inc.    ULTA
            "AVG",   // 288 AvalonBay Communities Inc.  AVB
            "VMC",   // 289 Vulcan Materials Company    VMC
            "FTV",   // 290 Fortive Corp.   FTV 0.069955       67.73    0.18    (0.26%)
            "EFX",   // 291 Equifax Inc.    EFX
            "ETR",   // 292 Entergy Corporation ETR
            "LUV",   // 293 Southwest Airlines Co.  LUV
            "FRC",   // 294 First Republic Bank FRC
            "NDAQ",  // 295 Nasdaq Inc. NDAQ
            "ARE",   // 296 Alexandria Real Estate Equities Inc.    ARE
            "AEE",   // 297 Ameren Corporation  AEE
            "MLM",   // 298 Martin Marietta Materials Inc.  MLM
            "RJF",   // 299 Raymond James Financial Inc.    RJF
            "DAL",   // 300 Delta Air Lines Inc.    DAL
            "LEN",   // 301 Lennar Corporation Class A  LEN
            "FE",    // 302 FirstEnergy Corp.   FE
            "DTE",   // 303 DTE Energy Company  DTE
            "HBAN",  // 304 Huntington Bancshares Incorporated  HBAN
            "CTRA",  // 305 Coterra Energy Inc. CTRA
            "ANSS",  // 306 ANSYS Inc.  ANSS
            "EQR",   // 307 Equity Residential  EQR
            "RF",    // 308 Regions Financial Corporation   RF
            "CAH",   // 309 Cardinal Health Inc.    CAH
            "ACGL",  // 310 Arch Capital Group Ltd. ACGL
            "LH",    // 311 Laboratory Corporation of America Holdings  LH
            "HPE",   // 312 Hewlett Packard Enterprise Co.  HPE
            "PPL",   // 313 PPL Corporation PPL
            "IR",    // 314 Ingersoll Rand Inc. IR
            "LYB",   // 315 LyondellBasell Industries NV    LYB
            "CF",    // 316 CF Industries Holdings Inc. CF
            "EXR",   // 317 Extra Space Storage Inc.    EXR
            "PWR",   // 318 Quanta Services Inc.    PWR
            "EPAM",  // 319 EPAM Systems Inc.   EPAM
            "MKC",   // 320 McCormick & Company Incorporated    MKC
            "CFG",   // 321 Citizens Financial Group Inc.   CFG
            "PFG",   // 322 Principal Financial Group Inc.  PFG
            "MRO",   // 323 Marathon Oil Corporation    MRO
            "WAT",   // 324 Waters Corporation  WAT
            "DOV",   // 325 Dover Corporation   DOV
            "XYL",   // 326 Xylem Inc.  XYL
            "CHD",   // 327 Church & Dwight Co. Inc.    CHD
            "MOH",   // 328 Molina Healthcare Inc.  MOH
            "TDY",   // 329 Teledyne Technologies Incorporated  TDY
            "CNP",   // 330 CenterPoint Energy Inc. CNP
            "TSN",   // 331 Tyson Foods Inc. Class A    TSN
            "NTRS",  // 332 Northern Trust Corporation  NTRS
            "AES",   // 333 AES Corporation AES
            "HOLX",  // 334 Hologic Inc.    HOLX
            "EXPD",  // 335 Expeditors International of Washington Inc. EXPD
            "INVH",  // 336 Invitation Homes Inc.   INVH
            "MAA",   // 337 Mid-America Apartment Communities Inc.  MAA
            "VRSN",  // 338 VeriSign Inc.   VRSN
            "AMCR",  // 339 Amcor PLC   AMCR
            "STE",   // 340 STERIS Plc  STE
            "VTR",   // 341 Ventas Inc. VTR
            "WAB",   // 342 Westinghouse Air Brake Technologies Corporation WAB
            "K",     // 343 Kellogg Company K
            "SYF",   // 344 Synchrony Financial SYF
            "CLX",   // 345 Clorox Company  CLX
            "CAG",   // 346 Conagra Brands Inc. CAG
            "DRI",   // 347 Darden Restaurants Inc. DRI
            "IEX",   // 348 IDEX Corporation    IEX
            "MOS",   // 349 Mosaic Company  MOS
            "DGX",   // 350 Quest Diagnostics Incorporated  DGX
            "CINF",  // 351 Cincinnati Financial Corporation    CINF
            "BALL",  // 352 Ball Corporation    BALL
            "CMS",   // 353 CMS Energy Corporation  CMS
            "PKI",   // 354 PerkinElmer Inc.    PKI
            "KEY",   // 355 KeyCorp KEY
            "FDS",   // 356 FactSet Research Systems Inc.   FDS
            "BBY",   // 357 Best Buy Co. Inc.   BBY
            "WST",   // 358 West Pharmaceutical Services Inc.   WST
            "BR",    // 359 Broadridge Financial Solutions Inc. BR
            "ABMD",  // 360 ABIOMED Inc.    ABMD
            "MPWR",  // 361 Monolithic Power Systems Inc.   MPWR
            "TRGP",  // 362 Targa Resources Corp.   TRGP
            "ATO",   // 363 Atmos Energy Corporation    ATO
            "ETSY",  // 364 Etsy Inc.   ETSY
            "TTWO",  // 365 Take-Two Interactive Software Inc.  TTWO
            "SJM",   // 366 J.M. Smucker Company    SJM
            "SEDG",  // 367 SolarEdge Technologies Inc. SEDG
            "FMC",   // 368 FMC Corporation FMC
            "OMC",   // 369 Omnicom Group Inc   OMC
            "J",     // 370 Jacobs Solutions Inc.   J
            "PAYC",  // 371 Paycom Software Inc.    PAYC
            "EXPE",  // 372 Expedia Group Inc.  EXPE
            "AVY",   // 373 Avery Dennison Corporation  AVY
            "IRM",   // 374 Iron Mountain Inc.  IRM
            "WRB",   // 375 W. R. Berkley Corporation   WRB
            "EQT",   // 376 EQT Corporation EQT
            "COO",   // 377 Cooper Companies Inc.   COO
            "LVS",   // 378 Las Vegas Sands Corp.   LVS
            "SWKS",  // 379 Skyworks Solutions Inc. SWKS
            "JBHT",  // 380 J.B. Hunt Transport Services Inc.   JBHT
            "APA",   // 381 APA Corp.   APA
            "TXT",   // 382 Textron Inc.    TXT
            "AKAM",  // 383 Akamai Technologies Inc.    AKAM
            "NTAP",  // 384 NetApp Inc. NTAP
            "LDOS",  // 385 Leidos Holdings Inc.    LDOS
            "TRMB",  // 386 Trimble Inc.    TRMB
            "INCY",  // 387 Incyte Corporation  INCY
            "FLT",   // 388 FLEETCOR Technologies Inc.  FLT
            "TER",   // 389 Teradyne Inc.   TER
            "GRMN",  // 390 Garmin Ltd. GRMN
            "NVR",   // 391 NVR Inc.    NVR
            "ALGN",  // 392 Align Technology Inc.   ALGN
            "MTCH",  // 393 Match Group Inc.    MTCH
            "ESS",   // 394 Essex Property Trust Inc.   ESS
            "LKQ",   // 395 LKQ Corporation LKQ
            "UAL",   // 396 United Airlines Holdings Inc.   UAL
            "KIM",   // 397 Kimco Realty Corporation    KIM
            "PEAK",  // 398 Healthpeak Properties Inc.  PEAK
            "ZBRA",  // 399 Zebra Technologies Corporation Class A  ZBRA
            "LNT",   // 400 Alliant Energy Corp LNT
            "DPZ",   // 401 Domino's Pizza Inc.	DPZ
            "HWM",   // 402 Howmet Aerospace Inc.   HWM
            "TYL",   // 403 Tyler Technologies Inc. TYL
            "JKHY",  // 404 Jack Henry & Associates Inc.    JKHY
            "BRO",   // 405 Brown & Brown Inc.  BRO
            "HRL",   // 406 Hormel Foods Corporation    HRL
            "SIVB",  // 407 SVB Financial Group SIVB
            "EVRG",  // 408 Evergy Inc. EVRG
            "IP",    // 409 International Paper Company IP
            "CBOE",  // 410 Cboe Global Markets Inc CBOE
            "IPG",   // 411 Interpublic Group of Companies Inc. IPG
            "PTC",   // 412 PTC Inc.    PTC
            "RE",    // 413 Everest Re Group Ltd.   RE
            "HST",   // 414 Host Hotels & Resorts Inc.  HST
            "GEN",   // 415 Gen Digital Inc.    GEN
            "BF.B",  // 416 Brown-Forman Corporation Class B    BF.B
            "VTRS",  // 417 Viatris Inc.    VTRS
            "RCL",   // 418 Royal Caribbean Group   RCL
            "TECH",  // 419 Bio-Techne Corporation  TECH
            "POOL",  // 420 Pool Corporation    POOL
            "SNA",   // 421 Snap-on Incorporated    SNA
            "PKG",   // 422 Packaging Corporation of America    PKG
            "CPT",   // 423 Camden Property Trust   CPT
            "NDSN",  // 424 Nordson Corporation NDSN
            "LW",    // 425 Lamb Weston Holdings Inc.   LW
            "CHRW",  // 426 C.H. Robinson Worldwide Inc.    CHRW
            "UDR",   // 427 UDR Inc.    UDR
            "SWK",   // 428 Stanley Black & Decker Inc. SWK
            "MGM",   // 429 MGM Resorts International   MGM
            "MAS",   // 430 Masco Corporation   MAS
            "WDC",   // 431 Western Digital Corporation WDC
            "CRL",   // 432 Charles River Laboratories International Inc.   CRL
            "L",     // 433 Loews Corporation   L
            "NI",    // 434 NiSource Inc    NI
            "HSIC",  // 435 Henry Schein Inc.   HSIC
            "KMX",   // 436 CarMax Inc. KMX
            "CPB",   // 437 Campbell Soup Company   CPB
            "GL",    // 438 Globe Life Inc. GL
            "TFX",   // 439 Teleflex Incorporated   TFX
            "CZR",   // 440 Caesars Entertainment Inc   CZR
            "JNPR",  // 441 Juniper Networks Inc.   JNPR
            "CE",    // 442 Celanese Corporation    CE
            "EMN",   // 443 Eastman Chemical Company    EMN
            "VFC",   // 444 V.F. Corporation    VFC
            "CDAY",  // 445 Ceridian HCM Holding Inc.   CDAY
            "PHM",   // 446 PulteGroup Inc. PHM
            "TAP",   // 447 Molson Coors Beverage Company Class B   TAP
            "LYV",   // 448 Live Nation Entertainment Inc.  LYV
            "STX",   // 449 Seagate Technology Holdings PLC STX
            "QRVO",  // 450 Qorvo Inc.  QRVO
            "BXP",   // 451 Boston Properties Inc.  BXP
            "PARA",  // 452 Paramount Global Class B    PARA
            "BWA",   // 453 BorgWarner Inc. BWA
            "ALLE",  // 454 Allegion Public Limited Company ALLE
            "MKTX",  // 455 MarketAxess Holdings Inc.   MKTX
            "REG",   // 456 Regency Centers Corporation REG
            "NRG",   // 457 NRG Energy Inc. NRG
            "FOXA",  // 458 Fox Corporation Class A FOXA
            "BBWI",  // 459 Bath & Body Works Inc.  BBWI
            "CCL",   // 460 Carnival Corporation    CCL
            "WRK",   // 461 WestRock Company    WRK
            "TPR",   // 462 Tapestry Inc.   TPR
            "CMA",   // 463 Comerica Incorporated   CMA
            "AAL",   // 464 American Airlines Group Inc.    AAL
            "HII",   // 465 Huntington Ingalls Industries Inc.  HII
            "FFIV",  // 466 F5 Inc. FFIV
            "AAP",   // 467 Advance Auto Parts Inc. AAP
            "ROL",   // 468 Rollins Inc.    ROL
            "CTLT",  // 469 Catalent Inc    CTLT
            "BIO",   // 470 Bio-Rad Laboratories Inc. Class A   BIO
            "PNW",   // 471 Pinnacle West Capital Corporation   PNW
            "WYNN",  // 472 Wynn Resorts Limited    WYNN
            "UHS",   // 473 Universal Health Services Inc. Class B  UHS
            "SBNY",  // 474 Signature Bank  SBNY
            "IVZ",   // 475 Invesco Ltd.    IVZ
            "RHI",   // 476 Robert Half International Inc.  RHI
            "FBHS",  // 477 Fortune Brands Home & Security Inc. FBHS
            "HAS",   // 478 Hasbro Inc. HAS
            "WHR",   // 479 Whirlpool Corporation   WHR
            "SEE",   // 480 Sealed Air Corporation  SEE
            "ZION",  // 481 Zions Bancorporation N.A.   ZION
            "AOS",   // 482 A. O. Smith Corporation AOS
            "FRT",   // 483 Federal Realty Investment Trust FRT
            "PNR",   // 484 Pentair plc PNR
            "NWSA",  // 485 News Corporation Class A    NWSA
            "BEN",   // 486 Franklin Resources Inc. BEN
            "AIZ",   // 487 Assurant Inc.   AIZ
            "DXC",   // 488 DXC Technology Co.  DXC
            "NCLH",  // 489 Norwegian Cruise Line Holdings Ltd. NCLH
            "GNRC",  // 490 Generac Holdings Inc.   GNRC
            "OGN",   // 491 Organon & Co.   OGN
            "XRAY",  // 492 DENTSPLY SIRONA Inc.    XRAY
            "LNC",   // 493 Lincoln National Corp   LNC
            "ALK",   // 494 Alaska Air Group Inc.   ALK
            "MHK",   // 495 Mohawk Industries Inc.  MHK
            "LUMN",  // 496 Lumen Technologies Inc. LUMN
            "NWL",   // 497 Newell Brands Inc   NWL
            "RL",    // 498 Ralph Lauren Corporation Class A    RL
            "FOX",   // 499 Fox Corporation Class B FOX
            "DVA",   // 500 DaVita Inc. DVA
            "DISH",  // 501 DISH Network Corporation Class A    DISH
            "VNO",   // 502 Vornado Realty Trust    VNO
            "NWS",   // 503 News Corporation Class B    NWS
        };
        #endregion
        #region static oex
        // for now, we just take the top-100 of spx
        #endregion
        #region static ndx
        private static List<string> _staticNdx = new List<string>
        {
            // see https://www.slickcharts.com/nasdaq100
            "AAPL",  // 1   Apple Inc   AAPL
            "MSFT",  // 2   Microsoft Corp  MSFT
            "AMZN",  // 3   Amazon.com Inc  AMZN
            "GOOG",  // 4   Alphabet Inc    GOOG
            "GOOGL", // 5   Alphabet Inc    GOOGL
            "TSLA",  // 6   Tesla Inc   TSLA
            "NVDA",  // 7   NVIDIA Corp NVDA
            "PEP",   // 8   PepsiCo Inc PEP
            "COST",  // 9   Costco Wholesale Corp   COST
            "META",  // 10  Meta Platforms Inc  META
            "AVGO",  // 11  Broadcom Inc    AVGO
            "CSCO",  // 12  Cisco Systems Inc   CSCO
            "TMUS",  // 13  T-Mobile US Inc TMUS
            "TXN",   // 14  Texas Instruments Inc   TXN
            "CMCSA", // 15  Comcast Corp    CMCSA
            "ADBE",  // 16  Adobe Inc   ADBE
            "AMGN",  // 17  Amgen Inc   AMGN
            "HON",   // 18  Honeywell International Inc HON
            "QCOM",  // 19  QUALCOMM Inc    QCOM
            "NFLX",  // 20  Netflix Inc NFLX
            "INTC",  // 21  Intel Corp  INTC
            "AMD",   // 22  Advanced Micro Devices Inc  AMD
            "SBUX",  // 23  Starbucks Corp  SBUX
            "GILD",  // 24  Gilead Sciences Inc GILD
            "INTU",  // 25  Intuit Inc  INTU
            "ADP",   // 26  Automatic Data Processing Inc   ADP
            "ISRG",  // 27  Intuitive Surgical Inc  ISRG
            "MDLZ",  // 28  Mondelez International Inc  MDLZ
            "PYPL",  // 29  PayPal Holdings Inc PYPL
            "AMAT",  // 30  Applied Materials Inc   AMAT
            "ADI",   // 31  Analog Devices Inc  ADI
            "VRTX",  // 32  Vertex Pharmaceuticals Inc  VRTX
            "BKNG",  // 33  Booking Holdings Inc    BKNG
            "REGN",  // 34  Regeneron Pharmaceuticals Inc   REGN
            "CSX",   // 35  CSX Corp    CSX
            "MRNA",  // 36  Moderna Inc MRNA
            "FISV",  // 37  Fiserv Inc  FISV
            "CHTR",  // 38  Charter Communications Inc  CHTR
            "MU",    // 39  Micron Technology Inc   MU
            "LRCX",  // 40  Lam Research Corp   LRCX
            "ATVI",  // 41  Activision Blizzard Inc ATVI
            "KDP",   // 42  Keurig Dr Pepper Inc    KDP
            "ORLY",  // 43  O'Reilly Automotive Inc	ORLY
            "KLAC",  // 44  KLA Corp    KLAC
            "MNST",  // 45  Monster Beverage Corp   MNST
            "MAR",   // 46  Marriott International Inc/MD   MAR
            "PANW",  // 47  Palo Alto Networks Inc  PANW
            "ASML",  // 48  ASML Holding NV ASML
            "SNPS",  // 49  Synopsys Inc    SNPS
            "AEP",   // 50  American Electric Power Co Inc  AEP
            "KHC",   // 51  Kraft Heinz Co/The  KHC
            "CTAS",  // 52  Cintas Corp CTAS
            "CDNS",  // 53  Cadence Design Systems Inc  CDNS
            "MELI",  // 54  MercadoLibre Inc    MELI
            "LULU",  // 55  Lululemon Athletica Inc LULU
            "DXCM",  // 56  Dexcom Inc  DXCM
            "NXPI",  // 57  NXP Semiconductors NV   NXPI
            "PAYX",  // 58  Paychex Inc PAYX
            "BIIB",  // 59  Biogen Inc  BIIB
            "ADSK",  // 60  Autodesk Inc    ADSK
            "ENPH",  // 61  Enphase Energy Inc  ENPH
            "MCHP",  // 62  Microchip Technology Inc    MCHP
            "ROST",  // 63  Ross Stores Inc ROST
            "FTNT",  // 64  Fortinet Inc    FTNT
            "EXC",   // 65  Exelon Corp EXC
            "AZN",   // 66  AstraZeneca PLC ADR AZN
            "ABNB",  // 67  Airbnb Inc  ABNB
            "XEL",   // 68  Xcel Energy Inc XEL
            "PDD",   // 69  Pinduoduo Inc ADR   PDD
            "MRVL",  // 70  Marvell Technology Inc  MRVL
            "PCAR",  // 71  PACCAR Inc  PCAR
            "WBA",   // 72  Walgreens Boots Alliance Inc    WBA
            "EA",    // 73  Electronic Arts Inc EA
            "IDXX",  // 74  IDEXX Laboratories Inc  IDXX
            "ILMN",  // 75  Illumina Inc    ILMN
            "DLTR",  // 76  Dollar Tree Inc DLTR
            "ODFL",  // 77  Old Dominion Freight Line Inc   ODFL
            "CTSH",  // 78  Cognizant Technology Solutions Corp CTSH
            "CEG",   // 79  Constellation Energy Corp   CEG
            "CPRT",  // 80  Copart Inc  CPRT
            "CRWD",  // 81  Crowdstrike Holdings Inc    CRWD
            "FAST",  // 82  Fastenal Co FAST
            "WDAY",  // 83  Workday Inc WDAY
            "VRSK",  // 84  Verisk Analytics Inc    VRSK
            "JD",    // 85  JD.com Inc ADR  JD
            "SIRI",  // 86  Sirius XM Holdings Inc  SIRI
            "EBAY",  // 87  eBay Inc    EBAY
            "SGEN",  // 88  Seagen Inc  SGEN
            "DDOG",  // 89  Datadog Inc DDOG
            "ANSS",  // 90  ANSYS Inc   ANSS
            "VRSN",  // 91  VeriSign Inc    VRSN
            "ZS",    // 92  Zscaler Inc ZS
            "BIDU",  // 93  Baidu Inc ADR   BIDU
            "ZM",    // 94  Zoom Video Communications Inc   ZM
            "TEAM",  // 95  Atlassian Corp  TEAM
            "LCID",  // 96  Lucid Group Inc LCID
            "ALGN",  // 97  Align Technology Inc    ALGN
            "SWKS",  // 98  Skyworks Solutions Inc  SWKS
            "MTCH",  // 99  Match Group Inc MTCH
            "SPLK",  // 100 Splunk Inc  SPLK
            "NTES",  // 101 NetEase Inc ADR NTES
            "DOCU",  // 102 DocuSign Inc    DOCU
        };
        #endregion
        #region static dji
        private static List<string> _staticDji = new List<string>
        {
            // as of 12/01/2022, see https://www.slickcharts.com/dowjones
            "UNH",   // 1   UnitedHealth Group Incorporated UNH
            "GS",    // 2   Goldman Sachs Group Inc.    GS
            "HD",    // 3   Home Depot Inc. HD
            "AMGN",  // 4   Amgen Inc.  AMGN
            "MCD",   // 5   McDonald's Corporation	MCD
            "MSFT",  // 6   Microsoft Corporation   MSFT
            "CAT",   // 7   Caterpillar Inc.    CAT
            "HON",   // 8   Honeywell International Inc.    HON
            "V",     // 9   Visa Inc. Class A   V
            "TRV",   // 10  Travelers Companies Inc.    TRV
            "CVX",   // 11  Chevron Corporation CVX
            "BA",    // 12  Boeing Company  BA
            "JNJ",   // 13  Johnson & Johnson   JNJ
            "CRM",   // 14  Salesforce Inc. CRM
            "AXP",   // 15  American Express Company    AXP
            "WMT",   // 16  Walmart Inc.    WMT
            "PG",    // 17  Procter & Gamble Company    PG
            "IBM",   // 18  International Business Machines Corporation IBM
            "AAPL",  // 19  Apple Inc.  AAPL
            "JPM",   // 20  JPMorgan Chase & Co.    JPM
            "MMM",   // 21  3M Company  MMM
            "MRK",   // 22  Merck & Co. Inc.    MRK
            "NKE",   // 23  NIKE Inc. Class B   NKE
            "DIS",   // 24  Walt Disney Company DIS
            "KO",    // 25  Coca-Cola Company   KO
            "DOW",   // 26  Dow Inc.    DOW
            "CSCO",  // 27  Cisco Systems Inc.  CSCO
            "WBA",   // 28  Walgreens Boots Alliance Inc.   WBA
            "VZ",    // 29  Verizon Communications Inc. VZ
            "INTC",  // 30  Intel Corporation   INTC
        };
        #endregion

        /// <summary>
        /// Return static universe. The constituents for these universes are
        /// time-invariant, hence suffering from survivorship bias.
        /// </summary>
        /// <param name="algo">parent algorithm</param>
        /// <param name="universe">universe name</param>
        /// <param name="datafeed">datafeed name</param>
        /// <returns>universe constituents</returns>
        /// <exception cref="Exception"></exception>
        public static HashSet<string> StaticGetUniverse(Algorithm algo, string universe, string datafeed)
        {
            var constituents = universe.ToLower() switch
            {
                "$spx" => _staticSpx,
                "$oex" => _staticSpx.Take(100),
                "$ndx" => _staticNdx,
                "$dji" => _staticDji,
                _ => throw new Exception(string.Format("Universe {0}:{1} not supported", datafeed, universe)),
            };

            return constituents
                .Select(name => datafeed + ':' + name)
                .ToHashSet();
        }
    }
}

//==============================================================================
// end of file
