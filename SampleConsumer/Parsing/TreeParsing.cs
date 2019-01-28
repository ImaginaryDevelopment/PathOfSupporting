using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CHelpers;
using static PathOfSupporting.Parsing.Trees;
using static PathOfSupporting.Parsing.Trees.Gems;
using PathOfSupporting.Parsing.PoB;

namespace SampleConsumer.Parsing
{
    public static class TreeParsing
    {

        // if you added this as a nuget package the files are included in the package folder, reference them there, or copy them to where you would like them to be
        // expecting to be running from PathOfSupporting/SampleConsumer/bin/debug/
        // target is ../../../PoS/
        // aka PathOfSupporting/PoS/
        internal static string GetResourcePath() => Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Environment.CurrentDirectory))), "PoS");
        internal static JsonResourcePath Rp => new JsonResourcePath(GetResourcePath(),filename: null);

        // things we can read out of the Path of Exile official Gems.json
        public static class Gems
        {

            public static void GetSkillGems()
            {
                var sgResult = getSkillGems(Rp);
                // have to check isOk since C#'s pattern matching to var will accept nulls
                if (sgResult.IsOk && sgResult.ResultValue.toList() is var sgs)
                {
                    Console.WriteLine("Found " + sgs.Count + " skill gem(s)");
                    foreach (var g in sgs)
                    {
                        Console.WriteLine("  " + g.Name + " - " + g.Level);
                    }
                }
                else
                {
                    Console.Error.WriteLine(sgResult.ErrorValue.Item1);
                }
            }
            public static void GetSkillGem()
            {
                var sgResult = getSkillGem(Rp, "Vaal Arc");
                if (sgResult.IsOk && sgResult.ResultValue is var g)
                {
                    Console.WriteLine("Found " + g.Name + " - " + g.Level);
                }
            }
            public static void GetGemReqLevels()
            {
                var result = getGemReqLevels(Rp, new[] { "Vaal Arc", "Essence Drain" });
                if (result.IsOk && result.ResultValue is var gs)
                {
                    foreach (var g in gs)
                        Console.WriteLine("Gem:" + g.ProvidedName + (g.LevelRequirement != null ? " req level" + g.LevelRequirement.Value : ""));
                }
            }

        }
        // things we can read out of the Path of Exile official passive tree javascript
        public static class Passives
        {
            public static void DecodeUrl()
            {
                var decodeResult = PassiveJsParsing.decodePassives(Rp, "https://www.pathofexile.com/fullscreen-passive-skill-tree/AAAABAMBAAceDXwOSBBREQ8RLxFQEZYTbRQJFLAVfhcvF1QYahslHNwdFB3ZJogo-isKK5osnCy_Ow09X0NUSVFKn0uuTC1Ms1JTUrJT1FXWVkhXyVgHXfJfKmTnZp5ncWnYa9ttGXRVdoJ4L3zlfqGApIIegseESIhCibyMdo2CjxqPRo_6knSVLpeVl_SYrZuhnwGg5qcIpyuo6qyYtUi3MLcxuJO6Drv8wcXG98gM0B_Q0NHk2CTZW9t634rfsONW6QLrY-vu7Bjsiu_r8NX0cffB99f56PrS");
                // have to check ok, or type out the type of result
                if (decodeResult.IsOk && decodeResult.GetOkOrNull() is var tree)
                {
                    Console.WriteLine(tree.Class);
                    foreach (var n in tree.Nodes.OrderBy(x => x.Name))
                    {
                        Console.WriteLine(n.Name + " - " + n.Effects);
                    }
                }
                else if (decodeResult.GetErrOrDefault() is var err)
                {
                    Console.Error.WriteLine(err.Item1);
                }
            }
        }
        public static class PathOfBuilding
        {
            const string sampleUrl = "https://pastebin.com/1Zsz702M";
            // Rusty Agony
            const string sampleCode = @"eNrtPWtz2ziSn1e_guWqvcqUxw7BN3PJXsmvxDPxYyznMftlihIhiwlFKiRlW7Pl_37dACmCJEhRijO3tXU7WzO23GgAjUa_G3r9P4_zULmnSRrE0Zs9cqjuKTSaxH4Q3b3Z-3B7duDs_c8_Bq-vvWx2NT1aBiH-5R-Dv71mPyshvafhmz3X3lMyL7mj2ccClf4HoBp7kR9kl3Ey9wDqMo5o8Vn1twuaTIKQpmnx8ST00vTSm9M3ezdedEeTPcVLJzTyj8s_4JqmQeTjH-deEI3iyVeavU3i5QJ2sqfcB_ThIvYB8vzi-urmtpjsOFnSYjWwk7-9vg69FU1GmZcpKfzrzd4QCOLd0RNvDv8GRF64BCymbZFDy7W0vZfyYaMFpf4a3DgkpmWbzwJ8ndDT6ZROsuCeHiewhZkXTcqVae6h3TZUAm5YOJumdw25WIZZsAgDJG4-jLTBv2vM4JLD1r3cxpkXnlyPytXrruEeurpq2a6-YVicrYepbZBHIVBWnEAnpnaoWo7rGrZtWq1EjoM0jsSBhkp09dDWTaLrpulsGFjlFs1yHNs8dDUTtucYbWM_BdmsOTFRDct03UPL0XXithL-wou84zjNhHGt7JaVB6mRVm65od-qkK1MckIfSwI7TgdCEZKYbhvkeSTswzQ7EPaEfB9MBZ53za6rOHmF0OfRpERsuF2IP0QJTWlyL95ht3st5ZBrEHdU3ISqdg28oXc0KoFt51B3uuDfUzqZvQWJeONlgvgCKUGI6TiaoZmW2cVT5Vy6SjqphtAi1Wy1C6-EaJbZc0SdZigyLBBjjqoZpmt1IanST7cOjS5oOfWIah7qtmuBqHJcYnRLqcp8GgqBNvhLmklOGMSV0bqj04gmd6vRLKChL4q4znMSB4nnpfaZpbq6XkPkRGwfeu-h1bAGdCzidu-HD6hcWL2dCSioBBji07omVJ9xyHUSf0EVHW45bpjM42WymUhs4xxY3LfWyozXs1UaTIAfmWa6of4SVhf3OcnC1riI7-kcbh0zVsCWKic9bNcMRyEYYrXdE-ew60DDUDaonWJZ5k2-nsT-XX8qs1lkQ_TWIWdBAlRLA0HBtptoxzFelJ7A74O7WRaBJV0bYbVKW1h0nNagNWfzyq_AlD32FsIt2byB-hin5zbq49Qem-k95jp-AMgZOhnpdtAXnmCptFIsodGfq974K-DiBFq7dPSXCTJe7znqI5r7eP2SeWH40_l8EScZ-_DYCycpQ3keLZaZEjFvaR6kkz_Gy-kUXaI9mCJhLt7p2dnp8e35x9N8FeKQ9GsQhn9Ey_kYvQD-39INGFEmTZRJHIbeIqX-m72pF6aAO4AfRzh2BEJ0kvWCBx8iN6D7QKMf0GsVzMnqA4k-T9-F5gKkNxluVwuK55r2GsLcl16Q3G_oBXp-FwVZT9rSibfqBblW8r2gL4ABuWLpRwcQ8kkwXma0Hziz9nutAw3cPoAVQ6vXirki74U7N3j6wOZSv9fZsSt0632l_c_khE4psHOaS5RCerxmrJsqPp16yzB7S-e_Lb0wyFZv9qIg3BM-f89jQOzTFIQQfJQercCdfbOXJcs8xsKwsTDNkFkW7Hc2F4vVhN4YcbwfKdZ7jEB54xCXysYraRijWRH7K6WwlWqIEAdO87fXMHkRlNJIgeltGI-9kBT47uj8HDdPM8_3Mu_leQYrfonLfsmw4Z7ghoN8HGWUhlUs2pqA3wp6aCqsEQdee0nGtsN-wzmqeNZA5cZrW0XBi3beeqQiDr0IIhZbGy3n8zgCnwgEUealc37W1c2rO2x-uVjw8yssx9KkzU2uPMbRlyA4U461H8qSPUtKfAwmQbxMlXJk-uwbvvEWgc8l3y6baw6XbWT0EEwzZTidhgE3xJ97Fx_jwAfxFiyWocct_R320oZEwqMIqlRgn3tHzA3jLHIVnS3DkAd1dthWJybZaXFYBWQnQiulfnnG7X2IxvEy8odBiF5WutPG6oMl55RPo6xBn3sfQ9-nPrPqv0NCtCGRbIiBKgxWEc3GZ9zSCfX8cPVdJyNHIdkOB_xxxwOECnYTBpWRslvCAZ57wefRJKEeWCtolqPWGIEt-JWm36OKc-ybUMs2uR6jFIOUYtRz7xwsZMA_2U0N1QfL9rKGeXblA4r5Nn4Mol0vyziAv7JwYbvCgTmUfJIfznPbirL-LLfZ5pFw3A-Sc2demtGE22I7HZwMgWxLHE7hgIVPxw3uLTwEzJcBFr3NS_hEvQWobK2ni_BdpGMhxdsY_vQMTNJEJuF_BqRwqB_DCMceBhi_ezdNZJLd5ByBUODpSDZEtN09OM4o227FELZSxSBZfw6wIycbbSz8joIZkPVjYPJdZgEQ_ioSYwabqCWzENqQSAiGsMrDjEa5MFOESEVlW_ru534OrnGSeUjvsPd-mnwsQyPZUQGmMLjmRqzdN4Lx-FGEofEtDqU-SHYIAKJwmG2DCsTYfTsfPS88CjFh8D3ipYlFZiUAkFJASffIPIe3cViI0e1v7zuaeEDHeKoM7-JopbTeZtjlPVqXPypixRdyNWXLqGw3n0frT29hXLnhNe1rM7WSv0aa2hFUsIwWgU-T6xCsPr-fQsvX3i94wKb8Do9UhkCyYQ62lYG21Tb-shhIwy_dNQSyzfbYXSyswh191AaCVm2NIdXbeDmZPf9G_jOCpltxphfMd2NEYaBs9ezvzx7UDrCEaLdzqAyVrTiH-D-P6G60zDvCu00mksd3t9eWrSmdOJOpRu15PLVC1VwvwYeW6kbSm579VGMxUw_dKFnUm73hY5CewsR0BEYacFSBlxVaPBdzFQ6Slxx5UbSNj3TO0tNx7KcZHBn1q-NlHASTKDnUc1-No9BLFzM6Xz2Du9rAJTm6EuZ5z-HjMgTyeCz6tWXY74SmCy9IOiPLHGLHe7spzEJ-YJglJw832c8nlJvrP_AS12dqj8IwQAUgleaiSidjjemZ7-0sSEIwY4Af_R4RcZFf5ENltlIOqBSQf236-Qz-E0xXu4QjakNlUacconYl8p9Y6cNtQqnicaZmY0hexAC_8K1-uHnPfvjbLMsW6auXLx8eHg4XXjaLp_QRjK3DSTx_ufDSFFAcsDUeZID05RD-dzQ8PxmeLe_P3n2cWc49IWfnb8_fRdrRSDu5_7S6uPvwdnj-7erXq6_nH917yzr6dvbH3dnZO_9K_eKm1L__NvW_nXwNb3QzoPTLp-NjU3U-_eJF3rdT7RO9O3l3NTSmzm8X46O73z5khjU__6yf0c-_XKvfPj14wR-uQezV2d3i3Sy8_UzI1ZKo8Tlxr7WHyy-_a39Emjr8nH6ZpLPwy-T04vFhuXIvZ1_GJrmd3N9--_P2n--XJ4uPc3-SjpPPiz8e_M-PQZYcHTunD9PhPx_Gwy9fIsPX7Ns_nMfx5PPQmATWl6PHX37Rxl9ug0_D4T8f08vPxoflY5BcaScXd2d_hMvJ9ec4G8Yn0w_j-dflufXtazq-pgfHj1_f_m5_OHe-PBihfU3vPhFVt9RHGmp06fyp-ffRw-_Zb8Mr7_xSffx1-nlxyw7oZXFCr3lbUMqPK_9NiWKfItfYrgVMBAqXcWVeitcEs4ijGyWc3QanOYZt9sCnWcS1esDpliXO2w6nu47bZ17iOn3wGZbjaL3oYpA-85oG0ewSrihClWyEWI7eB9A0idvn5AzHtpw-FNRsq9fEmuGSPpRRbV3gBE1rXSDRxIk7jtjSe02s6X3oohtGP0Jrlq1JWBrEZXmv4BcuGV-_RNGJP1zGGWV_G3nzRUgV_B3_XnzOZOzHgD4oKfWSyWyUJW_2UpCR4JD8Gcfz39_sHRg2tsU5GtFM2zLhFNNZ_PCOeuA-LtbKAIHzCjSTQ2Cp5UkwndKE1bUVGgABPwNWzXUOLcsCxgDedXnx_GumhnKRjz-PKDcUlykd0Ukc-dzgYR_ziXm1LIAq914SgCJkkTYsqjO4AXTjoYH_Svlwef7bh9PByYpVcy2jwc1yvFLOwJr8OvjIh74CN50q-qF6qJYfHS8T2EA24EW01FfWf9EGeSke_KgO2OZv6LdXiuUMzoHY4Pln6StFHfwrwdbJV-rTiwOiwv9_-rsSlHlFXlSMO_QH_8r38Io86aoINYT_orfCCzcVf8kCKGzx-WflWO0pn5A8vSDmgWb-tCUmTfF8P0A_0wvFwIUUGHnwIvZZd6jC5mX2AB5AwcuNvxP-96Isnxkf7edoSc_xU5BNZtMAzusooQ-DURaMsbT2hxyoUTlQMjgGGsL1UTxlNI-_UuU4jJc-huc-pHRgqn2Ot-CJQ_Pphake2FWmyAN-WJSv3AZz2v_ANfPA2AHXW0CQpdxqVDSi5P4K0iqlynCZeP0P_9As7t9Oxx8wxS478tECtoR9z8pNnA2O0RjNlF_oAw0HN_D5Es7mgsJ_54P3wTzAk81iPFTZZQRS2QdEr1_FdR0T8tdM8TIgCSbLDFU5jzIahsEdCjMYpPApf84THMrMS5Xq0b9jPdHKCRAP71J_jKcRnQfAMxwBYAI5lc-SecBumtm66g5ykw23LT81TUp6ICE2AMJCB1fR6lEZzpchzQanIe4Q8Qg3Lo-Vlp9gF4xw_eLQL39bt64IALghye00RRFrVG_k-lT3X6CItX6Co1c88KzL6vWW2zf0_VR5QbRi0AtNRZG53kZB2eZtK0fjEIOP1vUDHRDhlhsj9T4jWVqwPtKQrFhF9mUjDR1U9E8lKRvDzVJA5IOB9918uzpo4p-qXCSn1P4LAySVqapsIK8AF8lC_o56BZMTCutUpAmmk5QFcEjKdHcLJQAtYK3cROwNKJFgUgf7F0V68Ml4a4DCewM2ziqQ4YWutsjJuJCTIk9xCY3bnnuPwXw5Z9tsk-dN3KNsGSle5CusvQ4WOsFpVi1MtZ_z4N9xQsZJvPUAu11aaFgZwlhINsRoHVLyjmycQLh95B1NzydiTCMbQZ6ICsIwwV5HvMNibK04Xewn5pJRZCJxGOia-XzzKL02KonTbIy2QPsQozrkGPgEmzLvuycyq6MKJdkYgL048KnvU0bS61x0KtM4UYia82XKKzBW8VL5FWOAgMKDowaQD1HwbUmZGlj1kOgdKljbCKFvhDA2QpgbFL21EYO9EcLdMAchmwA204JsJgbZTA1ibgaxNi3WbreNTKmCvp2BGRrPkd2OPbg28LcB8GeWxCuQZyAZwa71KuZt7je-Uo4O1v8IGtbUq3bTvsbkBDMQQfLy0Wjl8EwKRg4HohB00NOpSsGKtB7oYMVMWAcf09X3mOMaU95qs4wigMfrQr3JrJiDx8o93hQNcHB1BvW7h2sTwVMlADs9fPBWqWKYf-91XTY5JI7cOs285GscffH-CzzU9L_BfANjdTAKwq9wy9_FsV91SrRDaxenZH1qbw_YP6JNpNZsokniTQFVzZWsmva5IW-KYo0rQiadgKDv44eatqs4mCraIbVz5k11qLPRsGuzJnQ0JphSPaGPGUVitirEUgGXsKTqIbOEvMI6TQdVC5nHres14wpvHxXVfM6zXXq-auLnmKsb7qQc2QlBh6j4KwS40y6N3OpduBnenA5gU_FcebsCq_84hquo3CAb5Frt_OSVYrvqVDeI5xtkYpjjCdXGVPemqjo2iGFPJ5ZqqhML_uPZhmW5LvVNz9cIdV2iesYE-BynZ5wPyES_wHSrd2BfMxunuW-wz0YY5LrLZgNXPBDRcN-33eZgXRc_Q_EzMKw-tmtxG5kFzlbg1p0MZlfzApoWchNVKnuOwI9coejLQA0coX85YJVCn1bJPAX8tFOG1OSIYcnDVyjVgX0PtJpYLzyBDtu6epsKfK6l5_5aboWiw3YaspcukGvWdmVaC6rwGETuaXBTWkGzf3D6mCWe8jZO6AYFvIHhO2wFl0WJy4u3SVkQuUM9isCTRyN1MExTzE9Fhdq4CLIsfZYAVoeuMJ22qAieMHG6BXopMS25Y6S1SWbml_FHG_LQ2KByeYaTyTLxJqtCEjbBScXSF_6yDrRIBgkC_NMMe1e8DO4pX3AFMv25uIAK7-ovgjGiDC8gZt49Ba0rmjH5oIdikrl0kp2MetJpo-T2a1MYv4sjuJ3KJ2w8HIAKnOP6eNCsIkWdknXUKm8Q0ukmE7U1GmW3yoB9cIMEGcx8YzEc1rbDWkz_Yvj2_HhwPIMjSrPiAo2W4WIGW-ZSAstffZgF9hZRUQl5FDSJZ_iq5ownpj2eGIRYZGJT2zMd3Rg7qjEmoIzcqWsSczJ1nSlRzYluT1QN9JZXIZ9ui-Qr75luyiPH4HCCF8hDFfhSYrQOH2uG4JaKwWOtoqeKF4Hya1UJztIiOCsloSlhkqsoDb0lBhhHM2Bt0BejbHUH7K58DNIK1Sxtarg6NT1Ht1RVdfSxq2MS1TCpoemmrxPP0lxKfH_qEapapmXTMTFUVddMbeL7VabTShk1rBLqnZcqRBmOVyAdw9zbQG2uC7GnfdeWKHdVGjbZ1-2OQEehm_eR-pIISgspLQkpTzBGDafiRYMRsNxXhRXXVbdttDliN-CEVUz6mjlTRrgUrYy24eVhVkcZ9FKCKYssPFAgw7sgQz0J3BKugBBm1Yzed4iEirqciuBCtVKRmO0MOrDMDoXSQl5bQt6zeBkCHSMMEGO-EtdwuqLbSbTcAqxsmZllxMLPNUOMwuInR2CIF4aZLcr7O4z0lLcHPVZjHd-BDWJkp4-RIHcpWY1fYdb96iXLYDCCmbNgGgAR2dnzdNd76t0t6SvlBCMATAn-rFwC-eFsnsXrFAwHtc1wMMEwrFgNa9OMc4Cggy_jmvE2XOATp6lUjNWDg4KxAFIgygosMEX-U1kljyoYaUejNRlrInLndElxcq705IbZn0ESFBrpOoFzATk6nNMMLP7sL8k6cwEh6mOFPRQEuEC2Mup0krvirbOI5gtN4wFjLJuteS6AkhvflQnzCWh7wpIjJjnzfC9iyZqJzt0LzBasXYvnWbT6fZh3i8uSZ47KyiSvJvcyh2FIwxir61agtJIJ3LlE4X1VnZHFVoWm1vLOWpHmZKE7QbZo6oHeL1vDvVQMSmldwUfSVV2BJiN-wgOIImYh1ytZUz3h-zso4IkXRXGmjOk6m7uj39k7Xt-RVtc0iUYdLUBSZcpHmqagQStZdVGdOma7g6B2nkw1W425b26zPmA-vLQkcmpbrT5D5S-5o7WmtS7xwuD_YPp0CfHcldfkMXXwK5JkOQ7ma9fcC-k0mDAH5TYJlvMFVt4q7705OC5eUle1FYG-lfbVu-M1N513qpSGP1eiqCiwNLW94qGZWSSsNsXpGlOgtxwdJrDaokPMRs2tPjF-YG-OuFaj10xvFXpMluTVftbrdkLbmB8VIuKlk_2FuPSqWlLjF_YRxwsxrFoxee2WO6qB62TJTV6N5XUMQ4x8MtccnzhI2bkVlq-mt_IBMSqWH1s0ylFezzdlwYfCeag7FayIRPTBmlHXFhrJHIRbOl9QkDHgFAxuH-KDURZHtEkqx5A77FotibJPjHLRSAyZ65MCmFaJKevtQZH9irtVVUv7tt70xTTS7nO1EMZpzRGezkG-z4Hex16C5X082tg3algT_Osk4FtWtQkUyqv_ec0STxZqqiI8FzOoV6KAeWZ3VKJsrsEQ00RYm-C21SaI1kStKLKuTXa599-fj9muLFKTex6_evFcyEAmGaZjEt6Div3dVY1EntvrwLhNFOcBm0rN1T5IZbVd0GtP-2b33ysHWEswiGmb3QSurspsozhU0D7ywgF7_1EiRzS5HHEqV0WvsamB1WYbUh6VCqkDq1ZPlMuZLqBKPFWz2gWSZUkCQFYjzbVPyIYlg1pKlouM7qpat69bkZ6k9F68jxOwab171Df0Ds02PMu5F4TbVEVU6w45u1sltxen4ahFmZ6B9d5MvMGhY31fR8qvwiNOURhYr2IUThzugVW9B71S3TkTmkX6Xa6L6qVi1ibhq6l1hIxtxAVbdZdJbilUM4714sC6sy2C_462ZpHZEViTKacXtsWM2RbOgkP7t6iaWkM8R92Us3tlVXlf1Y7LJnMpP6IJ8AjMF-BXRg2OktjzZxgWHyZJ_KD8tgzAOaxarm6LOVarvGeXiokmMLgkV0kM1HLzllm3XbeuEGstV6f8AqbBvispJtAtacZgX1O3tdp0ueA6oZGfwNJAdY-A7YMI1NAvHsuHbJHUN90dTDeiwg9pCh4TN5CqF1PH-Jc8Ucy8vapla6g9RU3RstJHOWIVv9moMysKmDC-kFt2eTqEZWzXJfg62BtC6VFH00zNPpSjA2LUc5jfpeJ62om7BPh0ecBjVFYh3NBxkICBUeurkRuBxNiiP4YFTz9EszhcgfeB4TRZZ8n5fL6MUDUAG_BH86XR6hs6x_CjcrRM2CWDOZZp6wWTdxIxZzrI4nX-OLiLvGyZ0MFJ4M0xmda1d82uXix8Sg04qxr4xaAD71-T7kIzW6NXUnim5epPoCp-rGCo0afwGWXB52rYuRrmFyM8dpXb12yO4Xi2mJSxexCxF7Ol-PJA7jVNWAiVP8Ylz0rv7PgUURV5d9CFF4KnvypO8RJZawoSZHAUYpaoonq6onJbJWN0UcSaLT0p2JSlm82GgJLOkjKTakuBU28brJQ71oJf7XB9vWlW56-tnZSuOv9GY4p5kPvzbM0dmpcLaNDYaXUDtQiBI1rQxNoWI2EEKW_XhMmmmmhiBZcYK5YtBLdWvaE9cVSPUGvrOPEwiQz64yjEDIEvqziquHkNo78vHjGu24qFkRC2kYf-8qewW5H_mPYE8p0acLMJTb678aBhHneB9O1O2BT00Vtqz7EqgusBzOd8pQcgtzcIPbNv4Gdj113B3vo2vui-rtZKv5uBQScPDILbqFUDgxK3mTApqf20SfrlVmaeImw3Sg2h0LDeZzbhFYfVpjTCi7un_DVdkJRJJutRm9RKErUnp_-o70pZPFtOWWpcycORZzRazpcp18rKJxaAqYSei9KVI5pmgZesdo1F5wbIOuY8TBd5LherPvgDl9W8snpoWw5Y-8C5xO7rxwgK0mjEO6oq9IV14Mq4EZGWaVAhaGQ7u8vlomeYd8dwJ07Pt12YRJ_ouBF_F7QrK5So3rJ1B04DEdosSNj2JjVG3u-MmW9mTCDad3tDhiz0e4Slsix3dOkFYcrFql-UO1RiFy3Z2psD9o_As3YtuUSqJZ34KmgR0-LlaA6eTM2ZqNXjVkTcvmXLs3wspUasZnVNzj7czK_odSmliIRSwwRR-T5wxCjyklrtaCVYbolRHnnpp9ZW-kkqCTbNbo9nO80kmu60XaniJjWLQOt5yhaSaJ3Mc0vxtdMOkuhblsDqdpUO7TnZfddp0sHdSId9Td82cmXobZWa1xhRArviYpmA_MAklLxY09E6YlX7htHYiLN1FWbFgr_10CcshGbuanM6dPbfGNIC9NNHcPADJF8pI8HoAjRByOoRi0p0fDSnsm_TaQup1JpwxHjIWULpnzwbfTzDlsiuyEgDOA-R6KbM5W_btqxo_IjOYA4g2RkP87YUJTiGaEGa9f6sJo_um0aFxfftZtjVPTQ7Xxoo0vbtEcRSLGpqq2nZQg1Zbca71SKKwdBVbmLPH7yNw8AD0rxN0OJJW6s06hmntxV9YdbsbfRkj9c8zL5Et5D_zDLgYUnOgfeUPRYCNkzDAi9rwYkrltMTokqqD6yWSnBNHvd25BEDTe0oDm8hsy1t9vDgJFfMSIPbhnZkWQfNGpMzuNqsLrxKdNKWwDarF63aaVZtLxMvIft6ULx1nWHJAqg1LtmoJyby91eiKB0vYR2ncOb5syujhzj5EU3FYiugXVXNoq_jdPRtid6NoXYC1ss2azGSxjMsa3iiPbF6D7Pd41MPNxdBs-CrpO74OI7g4PkjQlW9Wgk2ZbMA7WH2Ciu-lo0nPQuy3dOHROssVWhUgfw_v_Tkl_9Ibtlc1iLnj3ceSKgF2AOBB6edLNDywz5h5S0aRiHtXxNm6K3dwvt5UKhqsJqdxcKV3spa1NUDEboWqULpqWvK3j3Lz7YRHM0lcuHo1D8oH2QBqy6Mffozy6ggBDv6AdjzQVljwLtM0gbf4dKL6BGbAvVEe3FZn9oY2Ofm45Y_esg68qeACJMhVLmmXhIWb3gJ7qndHt4znAPT6vNGE0BvvkXvYzjJvIGCk3AdaWABBbwUGMFYYaKLV6LmY_LXuTaNKQy4_A0Q9u2IClZeAo8JMRDxccO7eifKZRwdVHYBq6fNpg60Ohn-7wvOdcQnTFl8gtnZ1QJX5QVaYy_XjttPg2Pu0r5S8OnNAUj8afD4Slmfq6E6T-tvuEOS2k0Y12KJHXxlmnM437bZhHR0UmLLI2hcapuD0XJaQ6s_4WK5hSj5OzHNp_VGOJTVhNIt62kYhmV5lC4vI6k_4FjKDrue6kJ7tr3Ol3kNdnv9SVlUokpqt8DCZqGJWvvjvm1I6kpMuX3d1cfaIjfs7pY2xmJyNXGVfvWS-eByOV6CKO1q9WkqBr1dMUheiOjQ862R1v0XcFcPeGa0o-KXzWgfOD-JyoQ7T_x7_HKRnT-Ik6sB3p1SlME0Hmnwkq9Ff4nssQN50oPJCk1VG7v94eWSPa1JU16CNJp49N4DLcIeqPyxJqSl_oUmJI-B2-aBq-VBcLAjiGn81G1HNuqDWnJcIKCq1mLVJqm3Yy7YS13-OtRH1H6FaWzg24SCs58P7T_xum-cGQb5cEeSxjuhU_aMMwP-NMOCnBy6UuDZbBeS3wSWLWev5MG_3oHAxUmFZ4fLm8qfTCzgr6ZTDv7D6zPdDVfGyK-M3OJi8nnsoWb2WGRwcJVEWDSwrjruqsPnH-nN_rGeZSnGNjXNht3WP_a04UE3sbermiUTyih-NqpNaFrTaduMh_xclLZs5XwJ7z3qO48lTzd0ihTmr65i0rgeL-SpG37JaJLW978er6sbh4qOA2Oi_FlgcT2CXwQ6iF-QsizlhNaeFhFFo6nuOBT3wfrqthn9PHpNarCYbQ9hgYk_ZvLM8-mgiBHy2Ec_BWTVQtQGjz93RiKwj9HdIhKRt463RCIctf2xjPxtoXl8j4tofaq9FgrGF2QUT4i7sqcU14HgMw_crFmc_cCuDFNefDmcCQ9fAbcPLgI_YmzPD7DfkdXM-y31P0o2dESZAcAeODOfyQA4YElL-UtTnT3oqPzV8nkk2OUySNjRDfbxCm-2CTibrKuUo7g-fc3CFl52rr3B9dD9YtaPf4HW6VcXasrrQvFBnvkUBBTmYavy4C_p124VLVq1U7undas_tQqkrvfNzSIQ90LnfT9bvm9u5KNNk9WNto6WvHFevvP5wjLRPOy8W83mzI4iLof3m28Qz1XAmvOtd-SlSHs8Ea1gw1SbLxn9J3UNbXQXnRbbFwuxaSHXPwas6_84WaYe5iEkbajf-8BhXmpTK7ep1xFU2Qz5rOshS845VssDBeqhsYWHtandhr9_QkyFf31ppW1Wq5Qvj2kOw8YJGl-8wKIhwKHZW8k_xhprMIW8EPAENp2m6yYLcCcDpiLZW00niXcXR_yl0meNLsgZw-oITW33hsU6PrWhO_IF8MuBemj9tEU8AB88kWf7U_a0TIgBcVbgEGFuAeGKBwPXtegIxwLbXtQAW1fGCNy2hq9W05DiWTkUew08kur5NkRaP0T_l-3D2v-34W3bhqce2qb1_K14Lbb2v1czHtv7lrpZynYtmbuEhg_4CuAIJMHXf1t_tvH4X4c_m0tT7lYAJzHvNVn19mXHlEuJ0n3lzQ7gMAKFzhO0xjDg8sgEYW2SHyc3DEeSpTudL1aw7UgZAeUWNBy885IFknyBDcXC1ydtyNSZtv30PgYc6wRbNR13jUo-yoz1yEswwaq_NFNsqvuEfWi3M9DMM5aMY1-CEkdaE1i17Cd-8fiEDRBS_Dm_lmK9lVvlzq6S6CLvtbkb23W7uNOpNG_MV7zjbr3V2nsYTTE1IFrXAzH_mizTLJ4_WWVFHqq3dTFB3ipZvHLG6j4eeN0HbI61aLJg2jwGke_nF0rAOSlxLnKxuQPGFkaVvdl_SR84I1YYdANX2irhXClJEDtNcMuyBXAWCOWwpAmrkz78bku42rUqjKpLssa6U2PWkpvZVeklV3fnXGJ2sq74LJcq_6vFEsYEHIZKyFrXNjG12c3UeQV87VU3rTNYueENtK5vstHkdbQxmFf3vI4WCxxuWWMCe0ZZeZGbNi8rlsuGQgdSLXKQcKZjWU-F7_-RhjGc6cpogqm6W-PKfDnDyOcL4uuRsKVtGU-iPeJI2NLUhJoICQAh2hPW4XIAu28i3m77XhinLSHeXkwsJsyFOmKDVXFX7C1X62mROpIv2ciLi5tFx7a0KEIzty4utuRPnJ55SbIMCyf1CL-fAQRiiOFXzFUKTy1v3a9mt5xC_m1F-Ogoa5rHx0V1ofew-e1inZ1ux15Wb3NDAw0DYNqWTW55DV_evC4rRuEvp7SmeFncWGhIa6x00HjEgFnx65cqxSxwEPllcUY1uA2Y1o83ZKAJ6w9jqfUnhJ6v1_lZg3sdslJen3NTbvwS-HHwIUnBMVWuaZj15kuzhS_x60rBOtUtaZ9IVZPIQ7SsMNNqPC_R3Rdc-4Ik-VePtESK5a9Jka7WA_HrRDAK4U2wDXcU_Ek3VWHu8hLvKIwzJfLmAMWLqYRvJi9irgJMXiksfN24aYlQ_Huyiy_W5mN4H4LwleKG0UTMSz-FyfthFRZi9BshfGM5cZvrwHRtrcOtMonebxZNGGK3klGAMiVQR7G_yn1TgTA9V2CUQ3SriZtVZwrz66psfjBwhCVqLWhEbpCgeUdBiglH68ipLlCsmCj_vvWO71oXJVEfXu7JzNLDGj14izprCBRUJUNl3NQ6ZKu708YoWy6w5drtcO96k4xsv3_Jnd3l0u5yayWcvCWJC1ooSIxeoKQ3yTZJk_7iZBd50mvtfamz0zDSg6htkq4p6rZcA5cx20qE9qtKdpluwyA5x5LdzoRs2ppUXUjPbSvB0K1qeuPX-pCqoaY2swnZrAMkuu1lrtzgF_5zij8dx9E0uOPKL1osCxwz5pFdTZk_9jEAhxA92hGauHBLl1hexh6NeNkYiVqTRbAxq4mu7J4yjuOQelF-yTuGsDBl8SURm8c9MPMcTuk2Xk5m3FinPlvkMX4tTrnSzoV-AIfhjsmczVNSXOF5ehSnwHhphpO_2bu8ujyVwI6X0-k69SdD_fplQf3XL6-9bHY1PVoGIQZP_zH4X84-q3k=";
            const string sampleMinionUrl = "https://pastebin.com/B43cypXN";
            public static void Parse(string codeOrUrl, Action<Character> actionOpt = null)
            {
                var result = PathOfBuildingParsing.processCodeOrPastebin(codeOrUrl);
                if (result.IsOk && result.GetOkOrNull() is var x)
                {
                    if (actionOpt is null)
                        Console.WriteLine(x.Class + " " + x.Ascendancy);
                    else actionOpt(x);
                }
                else
                {
                    Console.WriteLine(result.ErrorValue.Item1);
                }
            }
            public static void ParsePastebin() => Parse(sampleUrl, null);
            public static void ParseCode() => Parse(sampleCode, null);
            public static void ParseMinionPasteBin() => Parse(sampleMinionUrl, null);
        }
    }
}
