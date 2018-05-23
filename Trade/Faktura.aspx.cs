using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.BaseExtention.Enums.Controls;
using Kesco.Lib.BaseExtention.Enums.Docs;
using Kesco.Lib.Entities.Documents;
using Kesco.Lib.Entities.Documents.EF.Dogovora;
using Kesco.Lib.Entities.Documents.EF.Invoice;
using Kesco.Lib.Entities.Documents.EF.Trade;
using Kesco.Lib.Entities.Persons.PersonOld;
using Kesco.Lib.Entities.Resources;
using Kesco.Lib.Web.Controls.V4;
using Kesco.Lib.Web.Controls.V4.Common.DocumentPage;
using Kesco.Lib.Web.Controls.V4.Renderer;
using Kesco.Lib.Web.DBSelect.V4;
using Kesco.Lib.Web.DBSelect.V4.DSO;

namespace Trade
{
    /// <summary>
    ///  Новая форма работы со счет-фактурой
    /// </summary>
    /// <remarks>
    ///  Переписано с формы V3
    /// </remarks>
    public partial class Factura : DocPage
    {
        /// <summary>
        ///  Конструктор класса счет-фактура
        /// </summary>
        public Factura()
        {
            NextControlAfterNumber = "DateProvodki";
            NumberRequired = true;
        }

        /// <summary>
        ///  Текущий типизированный документ
        /// </summary>
        public SchetFactura SchetFactura { get { return (SchetFactura)Doc; } }
        private string _fpokname = "";

        private bool ShowFacturaPril { get; set; }

        /// <summary>
        ///  замена,  UserSettingsClass, Запоминает открытые/закрытые вкладки
        /// </summary>
        public NameValueCollection DisplaySettings = new NameValueCollection();

        /// <summary>
        ///  Установить параметры контролов: параметры, дефолтные значения и т.д.
        /// </summary>
        protected override void SetControlProperties()
        {
            flagCorrecting.IsReadOnly = true;

            // выставляем обязвательность полей
            DateProvodki.IsRequired = SchetFactura.DateProvodkiField.IsMandatory;
            DocumentOsnovanie.IsRequired = SchetFactura.OsnovanieField.IsMandatory;
            Currency.IsRequired = SchetFactura.CurrencyField.IsMandatory;
            Prodavets.IsRequired = SchetFactura.ProdavetsField.IsMandatory;
            Pokupatel.IsRequired = SchetFactura.PokupatelField.IsMandatory;
            Dogovor.IsRequired = SchetFactura.DogovorField.IsMandatory;

            // выставляем фильтры для селектров
            DocumentOsnovanie.Filter.Type.DocTypeParams.AddRange(GetControlTypeFilter(SchetFactura.OsnovanieField.DocFieldId));
            CorrectingDoc.Filter.Type.DocTypeParams.AddRange(GetControlTypeFilter(SchetFactura.CorrectingDocField.DocFieldId));
            Prodavets.Filter.PersonCheck = 1;
            Rukovoditel.Filter.PersonType = 2;
            Buhgalter.Filter.PersonType = 2;
            Pokupatel.Filter.PersonCheck = 1;
            Dogovor.Filter.Type.DocTypeParams.AddRange(GetControlTypeFilter(SchetFactura.DogovorField.DocFieldId));
            Prilozhenie.Filter.Type.DocTypeParams.AddRange(GetControlTypeFilter(SchetFactura.PrilozhenieField.DocFieldId));
            BillOfLading.Filter.Type.DocTypeParams.AddRange(GetControlTypeFilter(SchetFactura.BillOfLadingField.DocFieldId));
            Platezhki.Filter.Type.DocTypeParams.AddRange(GetControlTypeFilter(SchetFactura.PlatezhkiField.DocFieldId));
            Schet.Filter.Type.DocTypeParams.AddRange(GetControlTypeFilter(SchetFactura.SchetField.DocFieldId));
            Currency.Filter.CurrencyIDs.Value = "1";
        }

        /// <summary>
        ///  Установить режим только чтение
        /// </summary>
        private void SetReadOnly()
        {
            SupplierName.IsReadOnly = SupplierINN.IsReadOnly = SupplierAddress.IsReadOnly =
            ProdavetsName.IsReadOnly = ProdavetsINN.IsReadOnly = ProdavetsAddress.IsReadOnly =
            PokupatelName.IsReadOnly = PokupatelINN.IsReadOnly = PokupatelAddress.IsReadOnly =
                // DogovorText.IsReadOnly =
            GOPersonData.IsReadOnly = GPPersonData.IsReadOnly = true;

            flagCorrecting.IsReadOnly = true;// CorrectingDoc.IsReadOnly = true;

            DocumentOsnovanie.IsReadOnly =
            Supplier.IsReadOnly =
            Prodavets.IsReadOnly =
            Pokupatel.IsReadOnly =
            Dogovor.IsReadOnly = Prilozhenie.IsReadOnly = BillOfLading.IsReadOnly =
            Currency.IsReadOnly =
            Platezhki.IsReadOnly =
            Schet.IsReadOnly =
            GOPerson.IsReadOnly =
            GPPerson.IsReadOnly =
            SupplierKPP.IsReadOnly = ProdavetsKPP.IsReadOnly = PokupatelKPP.IsReadOnly =
                !DocEditable || SchetFactura._CorrectingDoc.Length > 0 || SchetFactura.IsCorrected;

            // Дата должна быть также редактируема как и номер счета-фактуры
            DateProvodki.IsReadOnly = Primechanie.IsReadOnly =
                Rukovoditel.IsReadOnly = Buhgalter.IsReadOnly = !DocEditable;
        }

        /// <summary>
        ///  Инициализация
        /// </summary>
        private void SetInitValue()
        {
            //Prodavets.WeakList.Add("1603"); // для тестов
            //Prodavets.WeakList.Add("28607");
            //Pokupatel.WeakList.AddRange(Prodavets.WeakList);
            //SchetFactura.WeakPersons = Prodavets.WeakList;

            if (!V4IsPostBack)
            {
                if (DocEditable && !IsPrintVersion)
                {
                    if (SchetFactura.IsNew || !IsInDocView)
                        V4SetFocus("DocNumber");
                }

                int docId = DocId;

                if (!SchetFactura.IsNew)
                {
                    var weakPersons = DocPersons.GetDocsPersonsByDocId(SchetFactura.DocId);
                    if (weakPersons.Count > 0)
                    {
                        var sWeakPers = SchetFactura.WeakPersons = weakPersons.Select(s => s.PersonId.ToString()).ToList();

                        Prodavets.WeakList.AddRange(sWeakPers);
                        Pokupatel.WeakList.AddRange(sWeakPers);
                    }
                }

                if (SchetFactura.IsNew || SchetFactura.DataUnavailable)
                {
                    if (docId != 0)
                    {
                        Document d = new Document(docId.ToString());
                        if (d.Unavailable) return;
                        if (d.DataUnavailable) return;

                        switch (d.Type)
                        {
                            case DocTypeEnum.Счет:
                                Schet_InfoFullClear();
                                Schet_InfoSet(docId.ToString());
                                return;
                            case DocTypeEnum.ТоварноТранспортнаяНакладная:
                            case DocTypeEnum.АктВыполненныхРаботУслуг:
                            case DocTypeEnum.Претензия:
                                if (DocumentOsnovanie_FacilityChange(d))
                                {
                                    SchetFactura._Osnovanie = d.Id;
                                    SetSchDataBy_DocumentOsnovanie(SchetFactura._Osnovanie);
                                    V3CS_RefreshSales();

                                    SetNumberByOsnovanie();
                                }

                                return;
                        }
                    }
                    else
                        FillSchByDogovor();
                }

                if (SchetFactura._Osnovanie.Length > 0)
                {
                    var d = new Document(SchetFactura._Osnovanie);

                    if (d.Unavailable) return;

                    DateProvodki.Value = SchetFactura.DateProvodkiField.ValueString;
                    //SetSchetByDocument(d);
                    SetBySchetFactura();
                    V3CS_RefreshSales();

                    DocumentOsnovanie.Value = SchetFactura._Osnovanie;
                    DocumentOsnovanie.ValueText = d.FullDocName;


                    switch (d.Type)
                    {
                        case DocTypeEnum.ТоварноТранспортнаяНакладная:
                            SetReadOnlyBY_TTN();
                            V3CS_RefreshDetailsSupplier(false);
                            break;
                        case DocTypeEnum.АктВыполненныхРаботУслуг:
                            SetReadOnlyBY_AktUsl();
                            V3CS_RefreshDetailsSupplier(true);
                            break;

                        case DocTypeEnum.Претензия:
                            SetReadOnlyBY_Claim();
                            V3CS_RefreshDetailsSupplier(false);
                            break;

                        default:
                            V3CS_RefreshDetailsSupplier(false);
                            break;
                    }
                }
                else
                {
                    var allBaseDocs = SchetFactura.GetBaseDocsAll();

                    // документ основание, привязанный вручную
                    var osnovanie = allBaseDocs.Where(d => d.Type == DocTypeEnum.АктВыполненныхРаботУслуг ||
                                                           d.Type == DocTypeEnum.Претензия ||
                                                           d.Type == DocTypeEnum.ТоварноТранспортнаяНакладная).ToList();

                    if (osnovanie.Count == 1)
                        SchetFactura._Osnovanie = osnovanie[0].Id;
                    else if (osnovanie.Count > 1)
                    {
                        var osnField = osnovanie.FirstOrDefault();
                        if (osnField != null) SchetFactura._Osnovanie = osnField.Id;
                    }

                    if (SchetFactura._Osnovanie.Length > 0)
                        DocumentOsnovanie_InfoSet("");
                }
                    
                V3CS_RefreshDetailsSupplier(false);
            }
        }

        private void SetBySchetFactura()
        {
            Currency.Value = SchetFactura.CurrencyField.ValueString;

            Supplier.Value = SchetFactura.SupplierField.ValueString;
            
            //Продавец;
            Prodavets.Value = SchetFactura.ProdavetsField.ValueString;
            ProdavetsAddress.Value = SchetFactura.ProdavetsAddressField.ValueString;
            Prodavets_InfoSet();


            Rukovoditel.ValueText = SchetFactura.RukovoditelTextField.ValueString;
            Buhgalter.ValueText = SchetFactura.BuhgalterTextField.ValueString;

            ShowDetails_Required("1");

            //Покупатель
            Pokupatel.Value = SchetFactura.PokupatelField.ValueString;
            Pokupatel_InfoSet();

            PokupatelAddress.Value = SchetFactura.PokupatelAddressField.ValueString;
            ShowDetails_Required("2");

            //Договор
            Dogovor.Value = SchetFactura._Dogovor;
            Prilozhenie.Value = SchetFactura._Prilozhenie;
            BillOfLading.Value = SchetFactura._BillOfLading;

            // устанавливаются в DocumentToControls
            //SchetFactura._Schets = nkl._SchetPred;
           // SchetFactura._Platezhki = nkl._Platezhki;

            //Грузоотправитель
            GOPerson_InfoClear();
            GOPerson.Value = SchetFactura.GOPersonField.ValueString;
            ShowDetails_Required("3");

            //Грузополучатель
            GPPerson_InfoClear();
            GPPerson.Value = SchetFactura.GPPersonField.ValueString;
  
            ShowDetails_Required("4");

            Primechanie.Value = SchetFactura.PrimechanieField.ValueString;

            RefreshAllFieldBind();

            CheckFieldDogovorUE();
        }

        /// <summary>
        ///  Заполнить счет на основании договора
        /// </summary>
        private void FillSchByDogovor()
        {
            var allBaseDocs = SchetFactura.GetBaseDocsAll();

            string _dogovor = "";
            string _dogovorText = "";
            int dCount = 0;
            string _prilozhenie = "";
            int p = 0;
            foreach (var bd in allBaseDocs)
            {
                var dogovorTypeId = (int)DocTypeEnum.Договор;
                if (bd.DocType.ChildOf(dogovorTypeId.ToString()))
                {
                    _dogovor = bd.Id;
                    _dogovorText = bd.FullDocName;

                    dCount++;
                }
                if (bd.Type == DocTypeEnum.ПриложениеКДоговору)
                {
                    _prilozhenie = bd.Id;
                    p++;
                }
            }

            if (dCount == 1)
            {
                var dog = _dogovor.ToInt();
                if (Doc.BaseDocs.Exists(b => b.BaseDocId == dog && b.DocFieldId == null))
                {
                    Doc.BaseDocs.RemoveAll(b => b.BaseDocId == dog && b.DocFieldId == null);
                }

                SchetFactura._Dogovor = _dogovor;
                SchetFactura.DogovorTextField.Value = _dogovorText;
                Dogovor_InfoSet();
            }
            if (p == 1)
            {
                SchetFactura._Prilozhenie = _prilozhenie;
                Prilozhenie_InfoSet();
            }

            if (SchetFactura._Dogovor.Length == 0 && SchetFactura._Prilozhenie.Length == 0)
            {
                if (Docdir != DocDirs.Undefined && CurrentPerson.Length > 0)
                {
                    if (Docdir == DocDirs.In)
                    {
                        SchetFactura.PokupatelField.Value = CurrentPerson.ToInt();
                        Pokupatel_InfoClear();
                        Pokupatel_InfoSet();
                        ShowDetails_Required("2");
                    }
                    else
                    {
                        SchetFactura.ProdavetsField.Value = CurrentPerson.ToInt();
                        Prodavets_InfoClear();
                        Prodavets_InfoSet();
                        ShowDetails_Required("1");
                    }

                }
            }
        }
        
        /// <summary>
        ///  Очистить документ
        /// </summary>
        private void ClearAll()
        {
            
            if (EntityId.IsNullEmptyOrZero())
            {
                JS.Write("location.reload();");
            }
            else
            {
                RefreshDocument();
            }
            //else
            //{
            //    DateProvodki.Value = "";
            //    SchetFactura.DateProvodkiField.ClearValue();

            //    Currency.Value = "";
            //    SchetFactura.CurrencyField.ClearValue();

            //    Supplier.Value = "";
            //    SchetFactura.SupplierField.ClearValue();
            //    SupplierName.Value = "";
            //    SchetFactura.SupplierNameField.ClearValue();
            //    SupplierINN.Value = "";
            //    SchetFactura.SupplierINNField.ClearValue();
            //    SupplierKPP.Value = "";
            //    SchetFactura.SupplierKPPField.ClearValue();
            //    SupplierAddress.Value = "";
            //    SchetFactura.SupplierAddressField.ClearValue();

            //    Prodavets.Value = "";
            //    SchetFactura.ProdavetsField.ClearValue();
            //    ProdavetsName.Value = "";
            //    SchetFactura.ProdavetsNameField.ClearValue();
            //    ProdavetsINN.Value = "";
            //    SchetFactura.ProdavetsINNField.ClearValue();
            //    ProdavetsKPP.Value = "";
            //    SchetFactura.ProdavetsKPPField.ClearValue();
            //    ProdavetsAddress.Value = "";
            //    SchetFactura.ProdavetsAddressField.ClearValue();

            //    Rukovoditel.Value = "";
            //    Rukovoditel.ValueText = "";
            //    SchetFactura.RukovoditelTextField.ClearValue();

            //    Buhgalter.Value = "";
            //    Buhgalter.ValueText = "";
            //    SchetFactura.BuhgalterTextField.ClearValue();

            //    Pokupatel.Value = "";
            //    SchetFactura.PokupatelField.ClearValue();
            //    PokupatelName.Value = "";
            //    SchetFactura.PokupatelNameField.ClearValue();
            //    PokupatelINN.Value = "";
            //    SchetFactura.PokupatelINNField.ClearValue();
            //    PokupatelKPP.Value = "";
            //    SchetFactura.PokupatelKPPField.ClearValue();
            //    PokupatelAddress.Value = "";
            //    SchetFactura.PokupatelAddressField.ClearValue();

            //    GOPerson.Value = "";
            //    SchetFactura.GOPersonField.ClearValue();
            //    GOPersonData.Value = "";
            //    SchetFactura.GOPersonDataField.ClearValue();
            //    GPPerson.Value = "";
            //    SchetFactura.GPPersonField.ClearValue();
            //    GPPersonData.Value = "";
            //    SchetFactura.GPPersonDataField.ClearValue();

            //    Dogovor.Value = "";
            //    SchetFactura.DogovorTextField.ClearValue();
            //    SchetFactura.DogovorField.ClearValue();
            //    SchetFactura._Dogovor = "";

            //    Prilozhenie.Value = "";
            //    SchetFactura.PrilozhenieField.ClearValue();
            //    SchetFactura._Prilozhenie = "";

            //    BillOfLading.Value = "";
            //    SchetFactura.BillOfLadingField.ClearValue();
            //    SchetFactura._BillOfLading = "";

            //    SchetFactura.PrimechanieField.ClearValue();
            //    Primechanie.Value = "";

            //    SchetFactura._Platezhki = "";
            //    SchetFactura._Schets = "";
            //    SchetFactura._CorrectingDoc = "";

            //    V3CS_RefreshSales();
            //    Platezhki_InfoFullClear();

            //    DisplaySettings["person1"] = "0";
            //    DisplaySettings["person2"] = "0";
            //    DisplaySettings["person3"] = "0";
            //    DisplaySettings["person4"] = "0";
            //    DisplaySettings["person5"] = "0";

            //    ShowDetails("1");
            //    ShowDetails("2");
            //    ShowDetails("3");
            //    ShowDetails("4");
            //    ShowDetails("5");
            //}
        }

        #region Override

        /// <summary>
        /// Процедура обработки клиентских запросов, вызывается с клиента либо синхронно, либо асинхронно
        /// </summary>
        /// <param name="cmd">Название команды</param>
        /// <param name="param">Коллекция параметров</param>
        protected override void ProcessCommand(string cmd, NameValueCollection param)
        {
            switch (cmd)
            {
                case "DownUp":
                    ShowDetails(param["arg0"]);
                    break;

                case "GetContact":
                    GetContact(param["arg0"], param["arg1"], param["arg2"]);
                    break;

                case "CheckOsn":
                    if (!DocEditable) return;
                    var scOsn = SchetFactura._Osnovanie;
                    if (scOsn.Length > 0)
                        SetSchDataBy_DocumentOsnovanie(scOsn);
                    else
                        V3CS_Alert("Не указан документ основание.");
                    break;

                case "DocumentOsnovanie_InfoSet":
                    DocumentOsnovanie_InfoSet(param["DocId"]);
                    break;

                case "DocumentOsnovanie_InfoClear":
                    DocumentOsnovanie_InfoClear(param["DocId"]);
                    break;

                case "ShowPril":
                    ShowFacturaPril = !ShowFacturaPril;
                    V3CS_RefreshFacturaPril();
                    break;

                case "flagCorrecting_Uncheck":
                    flagCorrecting_Uncheck();
                    break;

                case "ShowHideCorrectingDoc":
                    ShowHideCorrectingDoc(param["arg0"].ToBool());
                    break;

                case "CorrectingDoc_InfoClear":
                    CorrectingDoc_InfoClear(param["arg0"]);
                    break;

                case "CorrectingDoc_InfoSet":
                    CorrectingDoc_InfoSet();
                    break;

                case "RemoveBaseDoc":
                    RemoveBaseDoc(param["docId"].ToInt(), param["fieldId"].ToInt());
                    break;
                case "ClearAllDoc":
                    ClearAll();
                    break;
                case "ReturnOldValue":
                    ReturnOldValue(param["docId"]);
                    break;
                 default:
                    base.ProcessCommand(cmd, param);
                    break;
            }
        }

        /// <summary>
        ///  Загрузка страницы
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            NumberNotExists = false;
            ShowCopyButton = false;
            
            if (!V4IsPostBack)
            {
                if (SchetFactura._CorrectingDoc.Length > 0 || SchetFactura._CorrectingFlag == "1")
                    ShowHideCorrectingDoc(true);

                if (DisplaySettings["person1"] == null) DisplaySettings["person1"] = "0";
                if (DisplaySettings["person2"] == null) DisplaySettings["person2"] = "0";
                if (DisplaySettings["person3"] == null) DisplaySettings["person3"] = "0";
                if (DisplaySettings["person4"] == null) DisplaySettings["person4"] = "0";
                if (DisplaySettings["person5"] == null) DisplaySettings["person5"] = "0";

                ShowDetails("1");
                ShowDetails("2");
                ShowDetails("3");
                ShowDetails("4");
                ShowDetails("5");

                ShowFacturaPril = false;
            }

            
            //SetDataSource();
            //SetValidators();
            //SetConstFilter();
            SetHandlers();
            //SetControlFocusOrder();
            //SetMasterControl();
            SetReadOnly();
            SetInitValue();
        }

        /// <summary>
        ///  Проверка корректности вводимых полей
        /// </summary>
        /// <remarks>
        ///  Базовая валидация проходит только для полей со связью с колонками ДокументыДанные
        ///  Если валидация не требудется errors можно поставить null и вернуть true.
        /// </remarks>
        /// <param name="errors">Список ошибок, выходной параметр</param>
        /// <param name="exeptions">Исключения, Id поля для которого следует исключить валидацию</param>
        /// <returns>true - OK</returns>
        protected override bool ValidateDocument(out List<string> errors, params string[] exeptions)
        {
            bool result = base.ValidateDocument(out errors, exeptions);

            if (!Doc.BaseDocs.Exists(b => b.DocFieldId == SchetFactura.OsnovanieField.DocFieldId))
            {
                errors.Add("Не заполнено поле: Основание");
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Инициализация конкретного типа документа
        /// </summary>
        /// <param name="copy">Параметр указывается если копируем документ</param>
        protected override void DocumentInitialization(Document copy = null)
        {
            if (copy == null)
                Doc = new SchetFactura();
            else
                Doc = (SchetFactura)copy;

            if (Doc.IsNew)
                Doc.Date = DateTime.Today;

            SetBinders();
        }

        /// <summary>
        ///  Копирование данных документа на контролы. 
        /// </summary>
        protected override void DocumentToControls()
        {
            Schet.SelectedItems.AddRange(Doc.GetDocLinksItems(SchetFactura.SchetField.DocFieldId));
            Platezhki.SelectedItems.AddRange(Doc.GetDocLinksItems(SchetFactura.PlatezhkiField.DocFieldId));
        }

        /// <summary>
        ///  Объявить связь с контролами и данными
        /// </summary>
        private void SetBinders()
        {
            //DocDate. = new V3.Binder(sch, "_Date");
            flagCorrecting.BindDocField = SchetFactura.CorrectingFlagField;
           // CorrectingDoc.Binder = new V3.Binder(sch, "_CorrectingDoc");

           DateProvodki.BindDocField = SchetFactura.DateProvodkiField;

            Currency.BindDocField = SchetFactura.CurrencyField;
            DocumentOsnovanie.BindStringValue = SchetFactura.OsnovanieBind;

            Supplier.BindDocField = SchetFactura.SupplierField;
            SupplierName.BindDocField = SchetFactura.SupplierNameField;
            SupplierINN.BindDocField = SchetFactura.SupplierINNField;
            SupplierKPP.BindDocField = SchetFactura.SupplierKPPField;
            SupplierAddress.BindDocField = SchetFactura.SupplierAddressField;

            Prodavets.BindDocField = SchetFactura.ProdavetsField;
            ProdavetsName.BindDocField = SchetFactura.ProdavetsNameField;
            ProdavetsINN.BindDocField = SchetFactura.ProdavetsINNField;
            ProdavetsKPP.BindDocField = SchetFactura.ProdavetsKPPField;
            ProdavetsAddress.BindDocField = SchetFactura.ProdavetsAddressField;

            Pokupatel.BindDocField = SchetFactura.PokupatelField;
            PokupatelName.BindDocField = SchetFactura.PokupatelNameField;
            PokupatelINN.BindDocField = SchetFactura.PokupatelINNField;
            PokupatelKPP.BindDocField = SchetFactura.PokupatelKPPField;
            PokupatelAddress.BindDocField = SchetFactura.PokupatelAddressField;

            GOPerson.BindDocField = SchetFactura.GOPersonField;
            GOPersonData.BindDocField = SchetFactura.GOPersonDataField;
            GPPerson.BindDocField = SchetFactura.GPPersonField;
            GPPersonData.BindDocField = SchetFactura.GPPersonDataField;

            Dogovor.BindStringValue = SchetFactura.DogovorBind;

            Prilozhenie.BindStringValue = SchetFactura.PrilozhenieBind;

            BillOfLading.BindStringValue = SchetFactura.BillOfLadingBind;


            Primechanie.BindDocField = SchetFactura.PrimechanieField;
        }

        private void SetHandlers()
        {
          //  DocDate.V3EV_Changed += new V3.ChangedEventHandler(DateDoc_V3EV_Changed);
           // flagCorrecting.V3EV_Changed += new V3.ChangedEventHandler(flagCorrecting_V3EV_Changed);

            CorrectingDoc.OnRenderNtf += (sender, ntf) =>
            {
                ntf.Clear();
                if (string.IsNullOrEmpty(((V4Control)sender).Value)) return;
                ValidateBaseDocDate(SchetFactura.CorrectingDoc, ntf);
            };

            CorrectingDoc.BeforeSearch += CorrectingDocOnBeforeSearch;
          //  CorrectingDoc.V3EV_Changed += new V3.ChangedEventHandler(CorrectingDoc_V3EV_Changed);

           // DateProvodki.V3EV_Changed += new V3.ChangedEventHandler(DateProvodki_V3EV_Changed);

            #region Основание
            DocumentOsnovanie.OnRenderNtf += DocumentOsnovanieOnRenderNtf;
       //     DocumentOsnovanie.V3EV_Changed += new V3.ChangedEventHandler(DocumentOsnovanie_V3EV_Changed);
            DocumentOsnovanie.BeforeSearch += DocumentOsnovanieOnBeforeSearch;

            #endregion

            Currency.OnRenderNtf += CurrencyOnRenderNtf;
         //   Currency.V3EV_Changed += new V3.ChangedEventHandler(Currency_V3EV_Changed);

            #region Реальный поставщик

            Supplier.OnRenderNtf += SupplierOnRenderNtf;
            Supplier.BeforeSearch += PersonOnBeforeSearch;
          //  Supplier.V3EV_Changed += new V3.ChangedEventHandler(Supplier_V3EV_Changed);

            #endregion

            #region Продавец

            Prodavets.OnRenderNtf += ProdavetsOnRenderNtf;
            Prodavets.BeforeSearch += PersonOnBeforeSearch;
            Prodavets.Changed += ProdavetsOnChanged;

            Rukovoditel.OnRenderNtf += RukovoditelOnRenderNtf;
            Rukovoditel.BeforeSearch += PersonOnBeforeSearch;
            Rukovoditel.ValueChanged += RukovoditelOnChanged;
           // if (SchetFactura.RukovoditelTextField.IsMandatory) RukovoditelText.V3EV_Changed += new V3.ChangedEventHandler(RukovoditelText_V3EV_Changed);

             Buhgalter.OnRenderNtf += BuhgalterOnRenderNtf;
             Buhgalter.BeforeSearch += PersonOnBeforeSearch;
             Buhgalter.ValueChanged += BuhgalterOnValueChanged;
         //   if (sch.BuhgalterTextField.IsRequired) BuhgalterText.V3EV_Changed += new V3.ChangedEventHandler(BuhgalterText_V3EV_Changed);

            #endregion

            #region Покупатель
              Pokupatel.OnRenderNtf += PokupatelOnRenderNtf;
              Pokupatel.BeforeSearch += PersonOnBeforeSearch;
              Pokupatel.Changed += PokupatelOnChanged;
          //  fFullName.V3EV_Changed += new V3.ChangedEventHandler(fFullName_V3EV_Changed);

            #endregion

            #region Договор

            Dogovor.OnRenderNtf += DogovorOnRenderNtf;
            Dogovor.BeforeSearch += DogovorOnBeforeSearch;
            Dogovor.Changed += Dogovor_OnChanged;

            Prilozhenie.OnRenderNtf += PrilozhenieOnRenderNtf;
            Prilozhenie.BeforeSearch += PrilozhenieOnBeforeSearch;
          //  Prilozhenie.V3EV_Changed += new V3.ChangedEventHandler(Prilozhenie_V3EV_Changed);

            BillOfLading.OnRenderNtf += BillOfLadingOnOnRenderNtf;
            BillOfLading.BeforeSearch += BillOfLadingOnBeforeSearch;
            #endregion

            #region ГО

            GOPerson.OnRenderNtf += GoPersonOnRenderNtf;
            GOPerson.BeforeSearch += PersonOnBeforeSearch;
            GOPerson.Changed += GoPersonOnChanged;

            #endregion

            #region ГП

            GPPerson.OnRenderNtf += GpPersonOnRenderNtf;
            GPPerson.BeforeSearch += PersonOnBeforeSearch;
            GPPerson.Changed += GpPersonOnChanged;

            #endregion

            #region Платежки

             Platezhki.BeforeSearch += PlatezhkiOnBeforeSearch;
             Platezhki.ValueChanged += PlatezhkiOnValueChanged;

            #endregion

            #region Предоплата

            Schet.BeforeSearch += SchetOnBeforeSearch;
            //  Schet.V3EV_Changed += new V3.ChangedEventHandler(Schet_V3EV_Changed);


            #endregion

            //  if (sch.PrimechanieField.IsRequired) Primechanie.V3EV_Changed += new V3.ChangedEventHandler(Primechanie_V3EV_Changed);
        }

        /// <summary>
        ///  События изменения грузоотравителя
        /// </summary>
        private void GoPersonOnChanged(object sender, ProperyChangedEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.NewValue) && args.IsChange)
            {
                var p = new PersonOld(args.NewValue);
                SchetFactura.GOPersonDataField.Value = SetPerson_DataInfo(p);
            }
        }

        /// <summary>
        ///  Событие изменения 
        /// </summary>
        private void GpPersonOnChanged(object sender, ProperyChangedEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.NewValue) && args.IsChange)
            {
                var p = new PersonOld(args.NewValue);
                SchetFactura.GOPersonDataField.Value = SetPerson_DataInfo(p);
            }
        }

        /// <summary>
        ///  Событие при измении руководителя
        /// </summary>
        private void RukovoditelOnChanged(object sender, ValueChangedEventArgs valueArgs)
        {
            var rukControl = sender as DBSPerson;
            if (rukControl != null)
                SchetFactura.RukovoditelTextField.Value = rukControl.ValueText;
        }

        /// <summary>
        ///  Событие при измении бухгалтера
        /// </summary>
        private void BuhgalterOnValueChanged(object sender, ValueChangedEventArgs valueArgs)
        {
            var buhControl = sender as DBSPerson;
            if (buhControl != null)
                SchetFactura.BuhgalterTextField.Value = buhControl.ValueText;
        }

        /// <summary>
        ///  Валидация бухгалтера
        /// </summary>
        private void BuhgalterOnRenderNtf(object sender, Ntf ntf)
        {
            ntf.Clear();
            if (string.IsNullOrEmpty(((V4Control)sender).Value)) return;

            if (!Buhgalter.Value.IsNullEmptyOrZero())
            {
                var pers = new PersonOld(Buhgalter.Value);
            }
        }

        /// <summary>
        ///  Событие перед поиском счет
        /// </summary>
        private void SchetOnBeforeSearch(object sender)
        {
            Schet.Filter.PersonIDs.Clear();
            if (Prodavets.Value.Length > 0) Schet.Filter.PersonIDs.Add(Prodavets.Value);
            if (Pokupatel.Value.Length > 0) Schet.Filter.PersonIDs.Add(Pokupatel.Value);

            Schet.Filter.PersonIDs.UseAndOperator = true;

            Schet.Filter.Type.Clear();
            Schet.Filter.Type.Add(new DocTypeParam {DocTypeEnum = DocTypeEnum.Счет, QueryType = DocTypeQueryType.Equals});
            Schet.Filter.Type.Add(new DocTypeParam { DocTypeEnum = DocTypeEnum.ИнвойсПроформа, QueryType = DocTypeQueryType.Equals });

            
            //if (!string.IsNullOrEmpty(SchetFactura._Dogovor))
            //{
            //    Document d = new Document(SchetFactura._Dogovor);
            //    if (!d.Unavailable && d.DocType.ChildOf(DocTypeEnum.Договор))
            //    {
            //        Schet.Filter.LinkedDoc.Clear();
            //        if (!string.IsNullOrEmpty(Prilozhenie.Value)) Schet.Filter.LinkedDoc.Add(new LinkedDocParam { DocID = Prilozhenie.Value, QueryType = LinkedDocsType.AllСonsequences });
            //        if (!string.IsNullOrEmpty(Dogovor.Value)) Schet.Filter.LinkedDoc.Add(new LinkedDocParam { DocID = Dogovor.Value, QueryType = LinkedDocsType.AllСonsequences });
            //   }
            //}
        }

        /// <summary>
        ///  Событие перед поиском коносмент
        /// </summary>
        private void BillOfLadingOnBeforeSearch(object sender)
        {
            BillOfLading.Filter.PersonIDs.Clear();
            if (!SchetFactura.ProdavetsField.IsValueEmpty) BillOfLading.Filter.PersonIDs.Add(SchetFactura.ProdavetsField.ValueString);
            if (!SchetFactura.PokupatelField.IsValueEmpty) BillOfLading.Filter.PersonIDs.Add(SchetFactura.PokupatelField.ValueString);

            BillOfLading.Filter.PersonIDs.UseAndOperator = true;
        }

        /// <summary>
        ///  Событие перед поиском приложения
        /// </summary>
        private void PrilozhenieOnBeforeSearch(object sender)
        {
            Prilozhenie.Filter.PersonIDs.Clear();
            if (!SchetFactura.ProdavetsField.IsValueEmpty) Prilozhenie.Filter.PersonIDs.Add(SchetFactura.ProdavetsField.ValueString);
            if (!SchetFactura.PokupatelField.IsValueEmpty) Prilozhenie.Filter.PersonIDs.Add(SchetFactura.PokupatelField.ValueString);

            Prilozhenie.Filter.PersonIDs.UseAndOperator = true;
            
            Prilozhenie.Filter.LinkedDoc.Clear();
            if(!string.IsNullOrEmpty(Dogovor.Value))
                Prilozhenie.Filter.LinkedDoc.Add(new LinkedDocParam { DocID = Dogovor.Value, QueryType = LinkedDocsType.AllСonsequences });
        }

        /// <summary>
        /// Задание ограничений по сроку действия лица (срок действия лица "датаНачала...датаКонца" должен включать дату документа)
        /// </summary>
        protected void SetPersonRestrictions(DBSPerson control, DateTime docDate)
        {
            control.Filter.PersonValidAt = docDate == DateTime.MinValue ? DateTime.Today : docDate;
        }

        /// <summary>
        ///  Событие перед поиском корректируемого документа
        /// </summary>
        private void CorrectingDocOnBeforeSearch(object sender)
        {
            CorrectingDoc.Filter.PersonIDs.Clear();
            if (!SchetFactura.ProdavetsField.IsValueEmpty) CorrectingDoc.Filter.PersonIDs.Add(SchetFactura.ProdavetsField.ValueString);
            
            CorrectingDoc.Filter.LinkedDoc.Clear();
            if(!SchetFactura._Dogovor.IsNullEmptyOrZero())
                CorrectingDoc.Filter.LinkedDoc.Add(new LinkedDocParam { DocID = SchetFactura._Dogovor, QueryType = LinkedDocsType.AllСonsequences });
        }

        private void SchetOnRenderNtf(object sender, Ntf ntf)
        {
            ntf.Clear();
        }

        /// <summary>
        ///  Коносмент
        /// </summary>
        private void BillOfLadingOnOnRenderNtf(object sender, Ntf ntf)
        {
            ntf.Clear();
            if (string.IsNullOrEmpty(((V4Control)sender).Value)) return;

            var billOfland = SchetFactura.BillOfLading;
            ValidateBaseDocDate(billOfland, ntf);
        }

        /// <summary>
        /// Валидация документа приложения
        /// </summary>
        private void PrilozhenieOnRenderNtf(object sender, Ntf ntf)
        {
            ntf.Clear();
            if (string.IsNullOrEmpty(((V4Control)sender).Value)) return;

            if (SchetFactura._Prilozhenie.Length == 0)
			{
				Prilozhenie.ValueText = "";
				return;
			}

            var prilozhenie = SchetFactura.Prilozhenie;

			ValidateBaseDocDate(prilozhenie, ntf);

			if (prilozhenie.Unavailable)
			{
				ntf.Add(Resx.GetString("_Msg_ДокументНеДоступен"), NtfStatus.Error);
				return;
			}
			if (!prilozhenie.DocType.HasForm) return;
			if (prilozhenie.DataUnavailable)
			{
				ntf.Add(Resx.GetString("_Msg_ДокументНетЭФ"), NtfStatus.Error);
				return;
			}
            if (!prilozhenie.Signed) ntf.Add(Resx.GetString("ntf_DocIsNotSigned"), NtfStatus.Error);

            //if (!SchetFactura.CurrencyField.IsValueEmpty && !prilozhenie.ValyutaField.Equals(SchetFactura.CurrencyField))
            //    ntf.Add(Resx.GetString("_Msg_ПриложениеВалюта"), NtfStatus.Error);
        }

        private void RukovoditelOnRenderNtf(object sender, Ntf ntf)
        {
            ntf.Clear();
            if (string.IsNullOrEmpty(((V4Control)sender).Value)) return;

            if (!Rukovoditel.Value.IsNullEmptyOrZero())
            {
               // var pers = new PersonOld(Rukovoditel.Value);
            }
        }

        private void CheckKontrolDataSchByDataOsn(Ntf ntf, string _inx)
        {
            var osnovanie = SchetFactura.Osnovanie;
            if (osnovanie == null) return;
            if (osnovanie.Unavailable || osnovanie.DataUnavailable) return;

            if (osnovanie.Type == DocTypeEnum.ТоварноТранспортнаяНакладная)
            {
                TTN nkl = new TTN(SchetFactura._Osnovanie);
                switch (_inx)
                {
                    case "1":
                        if (!nkl.PostavschikField.Equals(SchetFactura.ProdavetsField))
                        {
                            ntf.Add(Resx.GetString("_Msg_ОснованиеПродавец"), NtfStatus.Error);
                            break;
                        }
                        if (!nkl.PostavschikAddressField.Equals(SchetFactura.ProdavetsAddressField))
                        {
                            ntf.Add(Resx.GetString("_Msg_ОснованиеПродавецАдрес"), NtfStatus.Error);
                        }
                        break;
                    case "2":
                        if (!nkl.PlatelschikField.Equals(SchetFactura.PokupatelField))
                        {
                            ntf.Add(Resx.GetString("_Msg_ОснованиеПокупатель"), NtfStatus.Error);
                            break;
                        }
                        if (!nkl.PlatelschikAddressField.Equals(SchetFactura.PokupatelAddressField))
                        {
                            ntf.Add(Resx.GetString("_Msg_ОснованиеПокупательАдрес"), NtfStatus.Error);
                        }
                        break;
                    case "3":
                        if (!nkl.GOPersonField.Equals(SchetFactura.GOPersonField))
                        {
                            ntf.Add(Resx.GetString("_Msg_ОснованиеГО"), NtfStatus.Error);
                        }
                        break;
                    case "4":
                        if (!nkl.GPPersonField.Equals(SchetFactura.GPPersonField))
                        {
                            ntf.Add(Resx.GetString("_Msg_ОснованиеГП"), NtfStatus.Error);
                        }
                        break;
                }

            }
            else if (osnovanie.Type == DocTypeEnum.АктВыполненныхРаботУслуг)
            {
                AktUsl akt = new AktUsl(SchetFactura._Osnovanie);
                switch (_inx)
                {
                    case "1":
                        if (!akt.IspolnitelField.Equals(SchetFactura.ProdavetsField))
                            ntf.Add(Resx.GetString("_Msg_ОснованиеИсполнитель"), NtfStatus.Error);
                        break;
                    case "2":
                        if (!akt.ZakazchikField.Equals(SchetFactura.PokupatelField))
                            ntf.Add(Resx.GetString("_Msg_ОснованиеЗаказчик"), NtfStatus.Error);
                        break;
                    case "3":
                        if (!akt.GOPersonField.Equals(SchetFactura.GOPersonField))
                        {
                            ntf.Add(Resx.GetString("_Msg_ОснованиеГО"), NtfStatus.Error);
                        }
                        break;
                    case "4":
                        if (!akt.GPPersonField.Equals(SchetFactura.GPPersonField))
                        {
                            ntf.Add(Resx.GetString("_Msg_ОснованиеГП"), NtfStatus.Error);
                            break;
                        }
                        break;
                }
            }
        }

        /// <summary>
        ///  Валидация реального поставщика
        /// </summary>
        private void SupplierOnRenderNtf(object sender, Ntf ntf)
        {
            ntf.Clear();
            if (string.IsNullOrEmpty(((V4Control)sender).Value)) return;

            var personId = SchetFactura.SupplierField.ValueString;
            string name = SchetFactura.SupplierNameField.ValueString;
            string inn = SchetFactura.SupplierINNField.ValueString;
            string kpp = SchetFactura.SupplierKPPField.ValueString;
            string address = SchetFactura.SupplierAddressField.ValueString;

            var p = new PersonOld(personId);
            var crd = p.GetCard(SchetFactura.Date == DateTime.MinValue ? DateTime.Today : SchetFactura.Date);

            Person_PreRender(Supplier, ntf, p, crd, personId, name, inn, kpp, address);
        }

        /// <summary>
        ///  Валидация валюты
        /// </summary>
        private void CurrencyOnRenderNtf(object sender, Ntf ntf)
        {
            ntf.Clear();
            if (string.IsNullOrEmpty(((V4Control)sender).Value)) return;
        }

        #endregion

        #region UE
        private void CheckFieldDogovorUE()
        {
            bool UE;
            CheckFieldDogovorUE(out UE);
        }

        private void CheckFieldDogovorUE(out bool UE)
        {
            UE = false;

            Document dc = null;
            if (!SchetFactura.DogovorField.IsValueEmpty)
            {
                var contrId = SchetFactura.DogovorField.ValueInt;
                dc = new Document(contrId.ToString());
            }


            if (dc != null && dc.Available && dc.DocumentData.Available)
            {

                var dogovorTypeId = (int)DocTypeEnum.Договор;
                if (dc.DocType.ChildOf(dogovorTypeId.ToString()))
                {
                    if (SchetFactura._Prilozhenie.Length > 0)
                    {
                        if (!SchetFactura.Prilozhenie.Unavailable && !SchetFactura.Prilozhenie.DataUnavailable)
                            UE = SchetFactura.Prilozhenie.UE;
                        else
                            UE = SchetFactura.Dogovor.UE;
                    }


                    Currency.IsReadOnly = UE;
                }
                else
                    Currency.IsReadOnly = UE;
            }
            else
                Currency.IsReadOnly = UE;


            if (!DocEditable || SchetFactura.CorrectingDocId > 0 || SchetFactura.IsCorrected) Currency.IsReadOnly = true;
        }

        private void SetKursAndScale(Hashtable ht)
        {
            decimal kurs;

            SchetFactura.KursField.Value = 0M;
            SchetFactura.FormulaDescrField.Value = "";

            var dc = SchetFactura.Dogovor;

            if (dc == null) return;
            if (dc.Unavailable || dc.DataUnavailable) return;


            SchetFactura.DogovorTextField.Value = SchetFactura.Dogovor.NameRusFull.Length > 0 ? SchetFactura.Dogovor.NameRusFull : SchetFactura.DogovorTextField.Value;

            var dogovorTypeId = (int)DocTypeEnum.Договор;
            if (!dc.DocType.ChildOf(dogovorTypeId.ToString())) return;

            if (!SchetFactura.PrilozhenieField.IsValueEmpty && !SchetFactura.Prilozhenie.Unavailable && !SchetFactura.Prilozhenie.DataUnavailable)
            {
                if (!SchetFactura.Prilozhenie.UE) return;

                kurs = SchetFactura.Prilozhenie.GetCoefUe2Valuta(SchetFactura.Date);

                SchetFactura.CurrencyField.Value = SchetFactura.Prilozhenie.Valyuta.ToInt();
                SchetFactura.KursField.Value = (kurs == 1) ? "" : Kesco.Lib.ConvertExtention.Convert.Decimal2Str(kurs, SchetFactura.CurrencyScale * 2);
                SchetFactura.FormulaDescrField.Value = (kurs == 1) ? "" : SchetFactura.FormulaDescrField.Value;
                return;
            }

            if (!SchetFactura.Dogovor.UE) return;

            kurs = SchetFactura.Dogovor.GetCoefUe2Valuta(SchetFactura.Date);
            SchetFactura.CurrencyField.Value = SchetFactura.Dogovor.Valyuta;
            SchetFactura.KursField.Value = (kurs == 1) ? "" : Kesco.Lib.ConvertExtention.Convert.Decimal2Str(kurs, SchetFactura.CurrencyScale * 2);
            SchetFactura.FormulaDescrField.Value = (kurs == 1) ? "" : SchetFactura.FormulaDescrField.Value;
        }

        #endregion

        #region ShowDetails

        /// <summary>
        ///  Расчет показа данных блока
        /// </summary>
        protected string pDisplay(string settingNumb, bool fl)
        {
            if (!DocEditable && fl) return "block";
            return DisplaySettings["person" + settingNumb] == "1" && fl ? "block" : "none";
        }

        /// <summary>
        ///  Обновить детали продавца
        /// </summary>
        private void V3CS_RefreshDetailsProdavets()
        {
            if (DocEditable && !IsPrintVersion && !IsInDocView)
            {
                JS.Write("ShowImageById('ProdavetsUpDown');");
                JS.Write(DisplaySettings["person1"] == "1" ? "upImageById('ProdavetsUpDown');" : "downImageById('ProdavetsUpDown');");
                JS.Write("showOrHideBlock('ProdavetsProp','{0}');", DisplaySettings["person1"] == "1" ? "block" : "none");
            }
            else
            {
                JS.Write("HideImageById('ProdavetsUpDown');");
                JS.Write("showOrHideBlock('ProdavetsProp','block');");
            }

            bool fl = true;
            for (int i = 0; i <= 6; i++)
            {
                switch (i)
                {
                    case 0:
                        fl = !SchetFactura.ProdavetsNameField.IsValueEmpty;
                        break;
                    case 1:
                        fl = !SchetFactura.ProdavetsINNField.IsValueEmpty;
                        break;
                    case 2:
                        fl = !SchetFactura.ProdavetsAddressField.IsValueEmpty;
                        break;
                    case 3:
                        break;
                    case 4:
                        fl = !SchetFactura.RukovoditelTextField.IsValueEmpty;
                        break;
                    case 5:
                        fl = !SchetFactura.BuhgalterTextField.IsValueEmpty;
                        break;
                    //case 6:
                    //    break;
                }
                JS.Write("showOrHideBlock('{0}','{1}');", "tr1_" + i, pDisplay("1", fl));

                fl = true;
            }
        }

        /// <summary>
        ///  Обновить детали покупателя
        /// </summary>
        private void V3CS_RefreshDetailsPokupatel()
        {
            if (DocEditable && !IsPrintVersion && !IsInDocView)
            {
                JS.Write("ShowImageById('PokupatelUpDown');");
                JS.Write(DisplaySettings["person2"] == "1" ? "upImageById('PokupatelUpDown');" : "downImageById('PokupatelUpDown');");
                JS.Write("showOrHideBlock('PokupatelProp','{0}');", DisplaySettings["person2"] == "1" ? "block" : "none");
            }
            else
            {
                JS.Write("HideImageById('PokupatelUpDown');");
                JS.Write("showOrHideBlock('PokupatelProp','block');");
            }


            bool fl = true;
            for (int i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        fl = !SchetFactura.PokupatelNameField.IsValueEmpty;
                        break;
                    case 1:
                        fl = !SchetFactura.PokupatelINNField.IsValueEmpty;
                        break;
                    //case 2:
                    //    fl = !SchetFactura.PokupatelAddressField.IsValueEmpty;
                    //    break;
                }
                JS.Write("showOrHideBlock('{0}','{1}');", "tr2_" + i, pDisplay("2", fl));
                fl = true;
            }
        }

        /// <summary>
        ///  Обновить детали грузоотправителя
        /// </summary>
        private void V3CS_RefreshDetailsGOPerson()
        {
            if (DocEditable && !IsPrintVersion && !IsInDocView)
            {
                JS.Write("ShowImageById('GOPersonUpDown');");
                JS.Write(DisplaySettings["person3"] == "1" ? "upImageById('GOPersonUpDown');" : "downImageById('GOPersonUpDown');");
                JS.Write("showOrHideBlock('GOPersonProp','{0}');", DisplaySettings["person3"] == "1" ? "block" : "none");
            }
            else
            {

                JS.Write("HideImageById('GOPersonUpDown');");
                JS.Write("showOrHideBlock('GOPersonProp','block');");
            }

            if (SchetFactura.GOPersonDataField.IsValueEmpty && !SchetFactura.GOPersonField.IsValueEmpty)
            {
                SchetFactura.GOPersonDataField.Value = SetPerson_DataInfo(SchetFactura.GOPerson);
            }


            bool fl = true;
            for (int i = 0; i < 1; i++)
            {
                switch (i)
                {
                    case 0:
                        fl = !SchetFactura.GOPersonDataField.IsValueEmpty;
                        break;
                }
                JS.Write("showOrHideBlock('{0}','{1}');", "tr3_" + i, pDisplay("3", fl));

                fl = true;
            }
        }

        /// <summary>
        ///  Обновить детали грузополучателя
        /// </summary>
        private void V3CS_RefreshDetailsGPPerson()
        {
            if (DocEditable && !IsPrintVersion && !IsInDocView)
            {
                JS.Write("ShowImageById('GPPersonUpDown');");
                JS.Write(DisplaySettings["person4"] == "1" ? "upImageById('GPPersonUpDown');" : "downImageById('GPPersonUpDown');");
                JS.Write("showOrHideBlock('GPPersonProp','{0}');", DisplaySettings["person4"] == "1" ? "block" : "none");
            }
            else
            {
                JS.Write("HideImageById('GPPersonUpDown');");
                JS.Write("showOrHideBlock('GPPersonProp','block');");
            }

            if (SchetFactura.GPPersonDataField.IsValueEmpty && !SchetFactura.GPPersonField.IsValueEmpty)
            {
                SchetFactura.GPPersonDataField.Value = SetPerson_DataInfo(SchetFactura.GPPerson);
            }

            bool fl = true;
            for (int i = 0; i < 1; i++)
            {
                switch (i)
                {
                    case 0:
                        fl = !SchetFactura.GPPersonDataField.IsValueEmpty;
                        break;
                }
                JS.Write("showOrHideBlock('{0}','{1}');", "tr4_" + i, pDisplay("4", fl));
                fl = true;
            }
        }

        /// <summary>
        ///  Обновить детали реального поставщика
        /// </summary>
        private void V3CS_RefreshDetailsSupplier(bool showFirstRow)
        {
            try
            {
                if (DocEditable && !IsPrintVersion && !IsInDocView)
                {
                    JS.Write("ShowImageById('SupplierUpDown');");
                    JS.Write(DisplaySettings["person5"] == "1" ? "upImageById('SupplierUpDown');" : "downImageById('SupplierUpDown');");
                    JS.Write("showOrHideBlock('SupplierProp','{0}');", DisplaySettings["person5"] == "1" ? "block" : "none");
                }
                else
                {
                    JS.Write("HideImageById('SupplierUpDown');");
                    if (!SchetFactura.SupplierField.IsValueEmpty)
                        JS.Write("showOrHideBlock('SupplierProp','block');");
                }


                bool fl = true;
                bool hasValues = !SchetFactura.SupplierNameField.IsValueEmpty || !SchetFactura.SupplierINNField.IsValueEmpty || !SchetFactura.SupplierKPPField.IsValueEmpty || !SchetFactura.SupplierAddressField.IsValueEmpty;
                for (int i = 0; i <= 3; i++)
                {
                    switch (i)
                    {
                        case 0:
                            fl = !SchetFactura.SupplierNameField.IsValueEmpty;
                            break;
                        case 1:
                            fl = !SchetFactura.SupplierINNField.IsValueEmpty || !SchetFactura.SupplierKPPField.IsValueEmpty;
                            break;
                        case 2:
                            fl = !SchetFactura.SupplierAddressField.IsValueEmpty;
                            break;
                        case 3:
                            fl = hasValues;
                            break;
                    }
                    JS.Write("showOrHideBlock('{0}','{1}');", "tr5_" + i, pDisplay("5", fl));
                    fl = true;
                }

                JS.Write("showOrHideBlock('tr_supplier', '{0}');", (!DocEditable ? hasValues && showFirstRow : showFirstRow) ? "block" : "none");
            }
            catch (Exception ex)
            {
                throw new Kesco.Lib.Log.DetailedException(ex.Message, ex, JS.ToString());
            }
        }

        /// <summary>
        ///  Показать раскрывающийся список в соответствии с ID
        /// </summary>
        private void ShowDetails(string inx, bool rebuild)
        {
            if (rebuild) DisplaySettings["person" + inx] = DisplaySettings["person" + inx] == "0" ? "1" : "0";

            switch (inx)
            {
                case "1":
                    V3CS_RefreshDetailsProdavets();
                    break;
                case "2":
                    V3CS_RefreshDetailsPokupatel();
                    break;
                case "3":
                    V3CS_RefreshDetailsGOPerson();
                    break;
                case "4":
                    V3CS_RefreshDetailsGPPerson();
                    break;
                case "5":
                    V3CS_RefreshDetailsSupplier(true);
                    break;
            }
        }

        private void ShowDetails(string inx)
        {
            ShowDetails(inx, V4IsPostBack);
        }

        /// <summary>
        ///  Показать раскрывающийся список
        /// </summary>
        private void ShowDetails_Required(string inx)
        {
            DisplaySettings["person" + inx] = "0";
            ShowDetails(inx, true);
        }

        #endregion

        #region Handlers

        #region Корректируемый документ

        /// <summary>
        ///  События изменения флага корректируемый документ
        /// </summary>
        protected void flagCorrecting_OnChanged(object sender, ProperyChangedEventArgs e)
        {
            var corDoc = SchetFactura._CorrectingDoc;
            if (!flagCorrecting.Checked && corDoc.Length > 0)
            {
                var sb = new StringBuilder();
                sb.Append("ВНИМАНИЕ!!!\nВы уверены, что хотите очистить ссылку на корректируемый документ?!\n");

                JS.Write("if (confirm({0})) cmd('cmd','flagCorrecting_Uncheck'); else cmd('cmd','ShowHideCorrectingDoc','arg0', 1);", sb);
                return;
            }

            ShowHideCorrectingDoc(flagCorrecting.Checked);
        }

        private void flagCorrecting_Uncheck()
        {
            ShowHideCorrectingDoc(false);
            //  SchetFactura._CorrectingDoc = ""; todo ------------------------todo--------todo setProperties
            SetReadOnly();
           // RefreshControlsReadOnly();
        }

        /// <summary>
        /// Показать/Скрыть корректируемый документ
        /// </summary>
        private void ShowHideCorrectingDoc(bool show)
        {
            JS.Write("gi('tdCorrectingDoc').style.display='{0}';", show ? "block" : "none");
            if (show)
            {
                flagCorrecting.Checked = true;
                CorrectingDoc.IsRequired = true;
            }
            else
                CorrectingDoc.IsRequired = false;

            CorrectingDoc.Flush();
        }

        /// <summary>
        ///  Событие перед началом поиска корректирующего документа
        /// </summary>
        protected void CorrectingDoc_OnBeforeSearch(object sender)
        {
            CorrectingDoc.Filter.PersonIDs.Value = SchetFactura.ProdavetsField.ValueString;

            CorrectingDoc.Filter.LinkedDoc.LinkedDocParams.Add(new LinkedDocParam { DocID = Dogovor.Value });
        }

        /// <summary>
        ///  Событие на изменение документа
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void CorrectingDoc_OnChanged(object sender, ProperyChangedEventArgs e)
        {
            var sb = new StringBuilder();
            sb.Append(Resx.GetString("msgOsnAttention0"));
            if (e.NewValue.Length > 0)
            {
                // Проверка того, что выбранный документ уже откорректирован. В этом случае предлагается заменить документ на вытекающий из него корректирующий счет
                SchetFactura correctingFaktura = new SchetFactura(e.NewValue);
                var seqCorDocs = correctingFaktura.GetSequelDocs(SchetFactura.CorrectingDocField.DocFieldId);
                if (seqCorDocs.Count > 1)
                {
                    V3CS_Alert(Resx.GetString("msgTwiceDocCorrect"));
                    CorrectingDoc_InfoClear(e.OldValue);
                }
                else if (seqCorDocs.Count == 1 && seqCorDocs[0].Id != SchetFactura.Id)
                {
                    string original = CorrectingDoc.ValueText;
                    do
                    {
                        SchetFactura._CorrectingDoc = seqCorDocs[0].Id;
                     //   correctingFaktura = new SchetFactura(seqCorDocs[0].Id);
                       // var col = correctingFaktura.GetSequelDocs(SchetFactura.CorrectingDocField.DocFieldId);
                    } while (seqCorDocs.Count >= 1 && seqCorDocs[0].Id != SchetFactura.Id);

                    sb.Append(string.Format(Resx.GetString("MSG_DocAlreadyCorrected"), original, CorrectingDoc.ValueText));
                    sb.Append(Resx.GetString("MSG_LastDocAsCorrected"));
                }
                else
                    sb.Append(string.Format(Resx.GetString("MSG_ChosenCorrected"), CorrectingDoc.ValueText));

                sb.Append(Resx.GetString("MSG_DataMergedWithCorrected"));
            }
            else
            {
                sb.Append(Resx.GetString("MSG_ConfirmUnlinkCorrected"));
            }
            JS.Write("if (confirm({1})) cmd('cmd','CorrectingDoc_InfoSet'); else cmd('cmd','CorrectingDoc_InfoClear','arg0', {0});", e.OldValue, sb);
        }

        /// <summary>
        /// Возврат старого значения корректируемого документа
        /// </summary>
        /// <param name="oldValue">Id старого документа (или "")</param>
        private void CorrectingDoc_InfoClear(string oldValue)
        {
            SchetFactura._CorrectingDoc = oldValue;
            CorrectingDoc.IsRequired = true;
        }

        /// <summary>
        /// Действия, выполняемые после изменения значения корректируемого документа
        /// </summary>
        private void CorrectingDoc_InfoSet()
        {
            var crDoc = SchetFactura._CorrectingDoc;
            if (crDoc.Length > 0)
                SetSchDataBy_Sch(crDoc);
            else
            {
                CorrectingDoc.IsRequired = true;
            }

            SetReadOnly();
            //RefreshControlsReadOnly(); 
        }

        #endregion

        #region Реальный поставщик

        /// <summary>
        ///  Очистить данные поставщика
        /// </summary>
        private void Supplier_InfoClear()
        {
            SchetFactura.SupplierNameField.ClearValue();
            SchetFactura.SupplierINNField.ClearValue();
            SchetFactura.SupplierKPPField.ClearValue();
            SchetFactura.SupplierAddressField.ClearValue();
        }

        #endregion

        /// <summary>
        ///  Валидация лица
        /// </summary>
        protected bool Person_PreRender(DBSPerson control, Ntf ntf, PersonOld p, PersonOld.Card crd, string personId, string name, string inn, string kpp, string address)
        {
            if (string.IsNullOrEmpty(personId))
            {
                control.ValueText = "";
                return false;
            }

            if (p.Unavailable)
            {
                ntf.Add(Resx.GetString("ntf_PersonIsNotAvailable"), NtfStatus.Error);
                return false;
            }

            if (!p.IsChecked) ntf.Add(Resx.GetString("_Msg_ЛицоНеПроверено"), NtfStatus.Error);

            if (string.IsNullOrEmpty(p.INN))
                ntf.Add(Resx.GetString("_Msg_ЛицоИНН"), NtfStatus.Error);

            if (crd != null)
            {
                if (crd.NameLat.Length == 0 && crd.NameRus.Length == 0)
                    ntf.Add(Resx.GetString("_Msg_ЛицоНазвание"), NtfStatus.Error);

                if (crd.КПП.Length == 0 && p.Type == 1)
                    ntf.Add(Resx.GetString("_Msg_ЛицоКПП"), NtfStatus.Error);

                if (address.Length == 0)
                    ntf.Add(Resx.GetString("_Msg_ЛицоАдрес"), NtfStatus.Error);

                if (crd.АдресЮридический.Length == 0 && crd.АдресЮридическийЛат.Length == 0)
                    ntf.Add(Resx.GetString("_Msg_ЛицоАдресЮридический"), NtfStatus.Error);
            }


            return true;
        }

        #region Продавец

        /// <summary>
        ///  Очистить данные продавца
        /// </summary>
        private void Prodavets_InfoClear()
        {
            SchetFactura.ProdavetsNameField.ClearValue();
            SchetFactura.ProdavetsAddressField.ClearValue();
            SchetFactura.ProdavetsINNField.ClearValue();
            SchetFactura.ProdavetsKPPField.ClearValue();

            ProdavetsAddress.Value = "";
            ProdavetsINN.Value = "";
            ProdavetsKPP.Value = "";

            Rukovoditel.Value = "";
            SchetFactura.RukovoditelTextField.ClearValue();
 
            Buhgalter.Value = "";
            SchetFactura.BuhgalterTextField.ClearValue();

            // закоментировал. По мнению Анисимова это неправильно
            //if (!SchetFactura.PlatezhkiField.IsValueEmpty) Platezhki_InfoClear(SchetFactura.ProdavetsField.ValueString);

            //if (SchetFactura._Prilozhenie.Length > 0 && SchetFactura.Prilozhenie.GetPersonIndex(SchetFactura.ProdavetsField.ValueString) == -1)
            //{
            //    SchetFactura._Prilozhenie = "";
            //}
            //if (!SchetFactura.DogovorField.IsValueEmpty && SchetFactura.Dogovor.GetPersonIndex(SchetFactura.ProdavetsField.ValueString) == -1)
            //{
            //    SchetFactura.DogovorTextField.ClearValue();
            //    SchetFactura.DogovorField.ClearValue();
            //}
        }

        /// <summary>
        ///  Установить данные продавца
        /// </summary>
        private void Prodavets_InfoSet()
        {
            if (V4IsPostBack)
            {
                V3CS_RefreshPlatezhkiData();
            }

            SetDocSigners();
            if (SchetFactura.ProdavetsField.IsValueEmpty) return;
            
            Schet_InfoFullClear();
            PersonOld p = new PersonOld(SchetFactura.ProdavetsField.ValueString);

            Prodavets.ValueText = p.Name;

            if (p.Unavailable) return;
            PersonOld.Card crd = p.GetCard(SchetFactura.Date == DateTime.MinValue ? DateTime.Today : SchetFactura.Date);
              if (crd == null) return;

              if (SchetFactura.ProdavetsINNField.IsValueEmpty) SchetFactura.ProdavetsINNField.Value = p.INN;
              if (SchetFactura.ProdavetsAddressField.IsValueEmpty) SchetFactura.ProdavetsAddressField.Value = string.IsNullOrEmpty(crd.АдресЮридический) ? crd.АдресЮридическийЛат : crd.АдресЮридический;
              if (SchetFactura.ProdavetsKPPField.IsValueEmpty) SchetFactura.ProdavetsKPPField.Value = crd.КПП;
              SchetFactura.ProdavetsNameField.Value = Person_GetName(SchetFactura.ProdavetsField.ValueString);
        }

        /// <summary>
        ///  Изменение значения продавца
        /// </summary>
        private void Prodavets_Changed()
        {
            Prodavets_InfoClear();
            Prodavets_InfoSet();
            ShowDetails_Required("1");
        }

        /// <summary>
        /// Событие изменения продавца
        /// </summary>
        private void ProdavetsOnChanged(object sender, ProperyChangedEventArgs e)
        {
            if (e.OldValue == e.NewValue) return;

            if (string.IsNullOrEmpty(e.NewValue))
            {
                Prodavets_InfoClear();
                ShowDetails_Required("1");
                return;
            }

            var weak = SchetFactura.WeakPersons;

            if (weak.Count > 0 && SchetFactura.PokupatelField.IsValueEmpty)
            {
                if (weak.Count == 2)
                {
                    Pokupatel.Value = weak.FirstOrDefault(w => w != e.NewValue) ?? "";
                    if (!string.IsNullOrEmpty(Pokupatel.Value))
                    {
                        SchetFactura.PokupatelField.Value = Pokupatel.Value.ToInt();  
                        Pokupatel_Changed(); 
                    }
                }
            }

            Prodavets_Changed();
        }

        /// <summary>
        ///  Валидация продавца 
        /// </summary>
        private void ProdavetsOnRenderNtf(object sender, Ntf ntf)
        {
            ntf.Clear();
            if (string.IsNullOrEmpty(((V4Control)sender).Value)) return;

            var prodavets = new PersonOld(SchetFactura.ProdavetsField.ValueString);
            var crd = prodavets.GetCard(SchetFactura.Date == DateTime.MinValue ? DateTime.Today : SchetFactura.Date);

            if (!Person_PreRender(Prodavets, ntf, prodavets, crd, SchetFactura.ProdavetsField.ValueString, SchetFactura.ProdavetsNameField.ValueString, SchetFactura.ProdavetsINNField.ValueString, SchetFactura.ProdavetsKPPField.ValueString, SchetFactura.ProdavetsAddressField.ValueString))
                return;

            if (crd != null && crd.Person.Type == 1)
                if (SchetFactura.ProdavetsNameField.IsValueEmpty || ((PersonOld.CardJ)crd).ПолноеНазвание.Length == 0)
                    ntf.Add(Resx.GetString("_Msg_ЛицоПолноеНазвание"), NtfStatus.Error);

            var dogovor = SchetFactura.Dogovor;
            if (dogovor != null && dogovor.Available && dogovor.DocumentData.Available)
                if (dogovor.GetPersonIndex(SchetFactura.ProdavetsField.ValueString) <= (dogovor.DataUnavailable ? -1 : 0))
                    ntf.Add(Resx.GetString("NTF_SmthIsNotMatchDogovor"), NtfStatus.Error);

            var prilozhenie = SchetFactura.Prilozhenie;
            if (prilozhenie != null && prilozhenie.Available && prilozhenie.DocumentData.Available)
                if (prilozhenie.GetPersonIndex(SchetFactura.ProdavetsField.ValueString) <= (prilozhenie.DataUnavailable ? -1 : 0))
                    ntf.Add(Resx.GetString("NTF_SmthIsNotMatchPrilozhenie"), NtfStatus.Error);

            CheckKontrolDataSchByDataOsn(ntf, "1");
        }

        /// <summary>
        ///  Валидация покупателя
        /// </summary>
        private void PokupatelOnRenderNtf(object sender, Ntf ntf)
        {
            ntf.Clear();
            if (string.IsNullOrEmpty(((V4Control)sender).Value)) return;

            var pokupatel = new PersonOld(SchetFactura.PokupatelField.ValueString);
            var crd = pokupatel.GetCard(SchetFactura.Date == DateTime.MinValue ? DateTime.Today : SchetFactura.Date);

            if (!Person_PreRender(Pokupatel, ntf, pokupatel, crd, SchetFactura.PokupatelField.ValueString, SchetFactura.PokupatelNameField.ValueString, SchetFactura.PokupatelINNField.ValueString, SchetFactura.PokupatelKPPField.ValueString, SchetFactura.PokupatelAddressField.ValueString))
                return;

            if (crd != null && crd.Person.Type == 1)
                if (SchetFactura.PokupatelNameField.IsValueEmpty || ((PersonOld.CardJ)crd).ПолноеНазвание.Length == 0)
                    ntf.Add(Resx.GetString("_Msg_ЛицоПолноеНазвание"), NtfStatus.Error);

            var dogovor = SchetFactura.Dogovor;
            if (dogovor != null && dogovor.Available && dogovor.DocumentData.Available)
                if (SchetFactura.Dogovor.GetPersonIndex(SchetFactura.PokupatelField.ValueString) <= (SchetFactura.Dogovor.DataUnavailable ? -1 : 0))
                    ntf.Add(Resx.GetString("NTF_SmthIsNotMatchDogovor"), NtfStatus.Error);

            var prilozhenie = SchetFactura.Prilozhenie;
            if (prilozhenie != null && prilozhenie.Available && prilozhenie.DocumentData.Available)
            if (SchetFactura.Prilozhenie.GetPersonIndex(SchetFactura.PokupatelField.ValueString) <= (SchetFactura.Prilozhenie.DataUnavailable ? -1 : 0))
                ntf.Add(Resx.GetString("NTF_SmthIsNotMatchPrilozhenie"), NtfStatus.Error);

            CheckKontrolDataSchByDataOsn(ntf, "2");
        }

        #endregion

        #region Покупатель

        private void Pokupatel_Changed()
        {
            Pokupatel_InfoClear();
            Pokupatel_InfoSet();
            ShowDetails_Required("2");
        }

        /// <summary>
        ///  Очистка данных покупателя
        /// </summary>
        private void Pokupatel_InfoClear()
        {
            SchetFactura.PokupatelNameField.ClearValue();
            SchetFactura.PokupatelAddressField.ClearValue();
            SchetFactura.PokupatelINNField.ClearValue();
            SchetFactura.PokupatelKPPField.ClearValue();

            // на всякий случай 
            PokupatelAddress.Value = "";
            PokupatelINN.Value = "";
            PokupatelKPP.Value = "";

            // закоментировал. По мнению Анисимова это неправильно
            //if (SchetFactura.Platezhki.Length > 0) Platezhki_InfoClear(SchetFactura.PokupatelField.ValueString);

            //if (SchetFactura._Prilozhenie.Length > 0 && SchetFactura.Prilozhenie.GetPersonIndex(SchetFactura.PokupatelField.ValueString) == -1)
            //{
            //    SchetFactura._Prilozhenie = "";
            //}
            //if (SchetFactura._Dogovor.Length > 0 && SchetFactura.Dogovor.GetPersonIndex(SchetFactura.PokupatelField.ValueString) == -1)
            //{
            //    SchetFactura.DogovorTextField.ClearValue();
            //    SchetFactura._Dogovor = "";
            //    Dogovor.RenderNtf();
            //}
        }

        private void Pokupatel_InfoSet()
        {
             Pokupatel_InfoSet(true); 
        }

        private void Pokupatel_InfoSet(bool clearSch)
        {
            if (V4IsPostBack)
            {
                V3CS_RefreshPlatezhkiData();
            }

            if (SchetFactura.PokupatelField.IsValueEmpty) return;        

            if (clearSch) Schet_InfoFullClear();
            PersonOld p = new PersonOld(SchetFactura.PokupatelField.ValueString);

            Pokupatel.ValueText = p.Name;
            
            if (p.Unavailable) return;
            PersonOld.Card crd = p.GetCard(SchetFactura.Date == DateTime.MinValue ? DateTime.Today : SchetFactura.Date);
            if (crd == null) return;

            if (SchetFactura.PokupatelINNField.IsValueEmpty || _fpokname == "1")
                SchetFactura.PokupatelINNField.Value = p.INN;

            SchetFactura.PokupatelNameField.Value = crd.NameRus.Length > 0 ? crd.NameRus : crd.NameLat;

            if (_fpokname == "1")
            {
                string fn = ((PersonOld.CardJ)crd).ПолноеНазвание;
                if (fn.Length > 0) SchetFactura.PokupatelNameField.Value = fn;
            }

            if (SchetFactura.PokupatelAddressField.IsValueEmpty || _fpokname == "1")
                SchetFactura.PokupatelAddressField.Value = string.IsNullOrEmpty(crd.АдресЮридический) ? crd.АдресЮридическийЛат : crd.АдресЮридический;
            if (SchetFactura.PokupatelKPPField.IsValueEmpty || _fpokname == "1")
                SchetFactura.PokupatelKPPField.Value = crd.КПП;
        }

        /// <summary>
        ///  Событие изменения покупателя
        /// </summary>
        private void PokupatelOnChanged(object sender, ProperyChangedEventArgs e)
        {
            if (e.OldValue == e.NewValue) return;
            
            if (string.IsNullOrEmpty(e.NewValue))
            {
                Pokupatel_InfoClear();
                ShowDetails_Required("2");
                return;
            }

            var weak = SchetFactura.WeakPersons;

            if (weak.Count > 0 && SchetFactura.ProdavetsField.IsValueEmpty)
            {
                if (weak.Count == 2 && !string.IsNullOrEmpty(e.NewValue))
                {
                    Prodavets.Value = weak.FirstOrDefault(w => w != e.NewValue) ?? "";
                    if (!string.IsNullOrEmpty(Prodavets.Value))
                    {
                        SchetFactura.ProdavetsField.Value = Prodavets.Value.ToInt();
                        Prodavets_Changed();
                    }
                }
            }

            Pokupatel_Changed();
        }

        #endregion

        /// <summary>
        /// Событие перед поиском персон
        /// </summary>
        private void PersonOnBeforeSearch(object sender)
        {
            var personControl = (DBSPerson) sender;
            SetPersonRestrictions(personControl, SchetFactura.Date);
            personControl.Filter.PersonCheck = 1;
        }

        private string Person_GetName(string personId)
        {
            string name = "";
            if (personId.Length > 0)
            {
                PersonOld p = new PersonOld(personId);
                if (p.Unavailable) return name;

                DateTime date = SchetFactura.Date == DateTime.MinValue ? DateTime.Today : SchetFactura.Date;

                PersonOld.Card crd = p.GetCard(date);
                if (crd == null) return name;

                var shortName = name = crd.NameRus.Length > 0 ? crd.NameRus : crd.NameLat;


                if (crd.Person.Type == 1)
                {
                    name = ((PersonOld.CardJ)crd).ПолноеНазвание;

                    if (date > new DateTime(2009, 6, 8) && date < new DateTime(2012, 6, 1))
                        name += " (" + shortName + ")";
                }

                if (string.IsNullOrEmpty(name))
                    name = shortName;
            }

            return name;
        }

        #region Договора

        /// <summary>
        ///  События изменения 
        /// </summary>
        protected void Dogovor_OnChanged(object sender, ProperyChangedEventArgs e)
        {
            if (e.NewValue != e.OldValue)
            {
                if (string.IsNullOrEmpty(e.NewValue))
                    Doc.RemoveAllBaseDocs(SchetFactura.DogovorField.DocFieldId);
                else
                    Doc.AddBaseDoc(e.NewValue, SchetFactura.DogovorField.DocFieldId);

                var control = (DBSDocument) sender;
                SchetFactura.DogovorTextField.Value = control.ValueText;

                RefreshDogovorKurator();
            }
        }

        /// <summary>
        ///  Очистить данные по договору
        /// </summary>
        private void Dogovor_InfoClear()
        {
            SchetFactura.DogovorTextField.ClearValue();
            SchetFactura.KursField.ClearValue();
        }

        /// <summary>
        ///  Вставить данные по договору
        /// </summary>
        private void Dogovor_InfoSet()
        {
            V3CS_RefreshKurs();
            V3CS_RefreshDogovorKurator();

            if (SchetFactura._Dogovor.Length > 0)
            {
                Schet_InfoFullClear();

                var d = SchetFactura.Dogovor;
                if (d.Unavailable) return;

                SchetFactura.DogovorTextField.Value = d.FullDocName;
                if (SchetFactura._Prilozhenie.Length == 0)
                {
                    Prilozhenie.TryFindSingleValue();
                    if (SchetFactura._Prilozhenie.Length != 0)
                    {
                        Prilozhenie_InfoSet();
                        return;
                    }
                }

                if (!d.DataUnavailable)
                {
                    //устанавливаем параметры, если они не установлены
                    if (SchetFactura.ProdavetsField.IsValueEmpty)
                    {
                        var prodavets = SchetFactura.PokupatelField.ValueString == d.Person1Field.ValueString ? d.Person2Field.Value : d.Person1Field.Value;
                        SchetFactura.ProdavetsField.Value = prodavets;
                        Prodavets_InfoSet();
                        ShowDetails_Required("1");
                    }
                    if (SchetFactura.PokupatelField.IsValueEmpty)
                    {
                        var pokupatel = SchetFactura.ProdavetsField.ValueString == d.Person2Field.ValueString ? d.Person1Field.Value : d.Person2Field.Value;
                        SchetFactura.PokupatelField.Value = pokupatel;
                        Pokupatel_InfoSet();
                        ShowDetails_Required("2");
                    }
                }
            }
        }

        /// <summary>
        ///  Установить данные приложения
        /// </summary>
        private void Prilozhenie_InfoSet()
        {
            if (SchetFactura._Prilozhenie.Length > 0)
            {
                Prilozhenie p = SchetFactura.Prilozhenie;
                if (!p.Unavailable && !p.DataUnavailable)
                {
                    //устанавливаем параметры, если они не установлены
                    if (SchetFactura.ProdavetsField.IsValueEmpty)
                    {
                        SchetFactura.ProdavetsField.Value = p.Person1Field.Value;
                        Prodavets_InfoSet();
                        ShowDetails_Required("1");
                    }
                    if (SchetFactura.PokupatelField.IsValueEmpty)
                    {
                        SchetFactura.PokupatelField.Value = p.Person2Field.Value;
                        Pokupatel_InfoSet();
                        ShowDetails_Required("2");
                    }
                }

                if (SchetFactura.DogovorField.IsValueEmpty) 
                    Dogovor.TryFindSingleValue();
            }
        }

        /// <summary>
        ///  Валидация договора
        /// </summary>
        private void DogovorOnRenderNtf(object sender, Ntf ntf)
        {
            ntf.Clear();
            if (string.IsNullOrEmpty(((V4Control)sender).Value)) return;

            if (SchetFactura._Dogovor.Length == 0)
            {
                return;
            }
            var dogovor = SchetFactura.Dogovor;
            ValidateBaseDocDate(dogovor, ntf);


            if (dogovor.Unavailable)
            {
                ntf.Add(Resx.GetString("_Msg_ДокументНеДоступен"), NtfStatus.Error);
                return;
            }
            if (dogovor.DataUnavailable)
            {
                ntf.Add(Resx.GetString("_Msg_ДокументНетЭФ"), NtfStatus.Error);
                return;
            }

            if (dogovor.DocType.ChildOf(DocTypeEnum.Договор))
            {
                if (!dogovor.IsValidAt(SchetFactura.Date == DateTime.MinValue ? DateTime.Today : SchetFactura.Date))
                    ntf.Add(Resx.GetString("_Msg_СрокДоговораФактура"), NtfStatus.Error);
            }
        }

        /// <summary>
        /// Событие перед поиском договор
        /// </summary>
        private void DogovorOnBeforeSearch(object sender)
        {
            Dogovor.Filter.PersonIDs.Clear();
            if (!SchetFactura.ProdavetsField.IsValueEmpty) Dogovor.Filter.PersonIDs.Add(SchetFactura.ProdavetsField.ValueString);
            if (!SchetFactura.PokupatelField.IsValueEmpty) Dogovor.Filter.PersonIDs.Add(SchetFactura.PokupatelField.ValueString);

            Dogovor.Filter.PersonIDs.UseAndOperator = true;

            Dogovor.Filter.LinkedDoc.Clear();
            if (!SchetFactura.PrilozhenieField.IsValueEmpty) Dogovor.Filter.LinkedDoc.Add(new LinkedDocParam { DocID = SchetFactura.PrilozhenieField.ValueString, QueryType = LinkedDocsType.AllСonsequences });
        }

        #endregion

        #region Платежки

        /// <summary>
        ///  Обработчик события изменения контрола платежек
        /// </summary>
        private void PlatezhkiOnValueChanged(object sender, ValueChangedEventArgs e)
        {
            if(string.IsNullOrEmpty(Platezhki.Value)) return;
            var id = Platezhki.Value;
            Platezhki_Changed(id);
            SetSchetByPlatezhki(id, false);
        }

        /// <summary>
        ///  Событие перед поиском платежки
        /// </summary>
        private void PlatezhkiOnBeforeSearch(object sender)
        {
            Platezhki.Filter.PersonIDs.Clear();
            if (Prodavets.Value.Length > 0) Platezhki.Filter.PersonIDs.Add(Prodavets.Value);
            if (Pokupatel.Value.Length > 0) Platezhki.Filter.PersonIDs.Add(Pokupatel.Value);

            Platezhki.Filter.PersonIDs.UseAndOperator = true;
        }

        /// <summary>
        /// Функция изменения платежек
        /// </summary>
        private void Platezhki_Changed(string id)
        {
            SchetFactura.AddBaseDoc(id, SchetFactura.PlatezhkiField.DocFieldId);
        }

        private void Platezhki_InfoClear(string p)
        {
            bool b = false;

            var platezhki = SchetFactura.Platezhki;
            foreach (Document d in platezhki)
            {
                if (d.GetPersonIndex(p) == -1)
                {
                    b = true;
                    break;
                }
            }
            if (b) SchetFactura.PlatezhkiField.ClearValue();
        }

        private void Platezhki_InfoFullClear()
        {
            SchetFactura.PlatezhkiField.ClearValue();
            V3CS_RefreshPlatezhkiData();
        }

        /// <summary>
        ///  Именение контрола платежек 
        /// </summary>
        protected void Platezhki_OnChanged(object sender, ProperyChangedEventArgs e)
        {
            if (!e.NewValue.IsNullEmptyOrZero())
            {
                Doc.AddBaseDoc(e.NewValue, SchetFactura.PlatezhkiField.DocFieldId);
            }
        }

        /// <summary>
        ///  Удаление из контрола платежек 
        /// </summary>
        protected void Platezhki_OnDeleted(object sender, ProperyDeletedEventArgs e)
        {
            if (!e.DelValue.IsNullEmptyOrZero())
            {
                Doc.RemoveBaseDoc(e.DelValue.ToInt(), SchetFactura.PlatezhkiField.DocFieldId);
            }
        }

        #endregion

        #region Счета

        /// <summary>
        /// Счет изменен
        /// </summary>
        protected void Schet_OnChanged(object sender, ProperyChangedEventArgs e)
        {
            if (!e.NewValue.IsNullEmptyOrZero())
            {
                Doc.AddBaseDoc(e.NewValue, SchetFactura.SchetField.DocFieldId);
            }
        }

        /// <summary>
        ///  Счет удален
        /// </summary>
        protected void Schet_OnDeleted(object sender, ProperyDeletedEventArgs e)
        {
            if (!e.DelValue.IsNullEmptyOrZero())
            {
                Doc.RemoveBaseDoc(e.DelValue.ToInt(), SchetFactura.SchetField.DocFieldId);
            }
        }

        #endregion

        #region ГО

        /// <summary>
        ///  Очистка данных грузоотправителя
        /// </summary>
        private void GOPerson_InfoClear()
        {
            SchetFactura.GOPersonDataField.ClearValue();
        }

        /// <summary>
        ///  Валидация Грузоотправителя
        /// </summary>
        private void GoPersonOnRenderNtf(object sender, Ntf ntf)
        {
            ntf.Clear();
            if (string.IsNullOrEmpty(((V4Control)sender).Value)) return;

            if (string.IsNullOrEmpty(((V4Control)sender).Value)) return;

            if (SchetFactura.GOPersonField.IsValueEmpty)
            {
                GOPerson.ValueText = "";
                return;
            }

            var p = SchetFactura.GOPerson;
            if (p.Unavailable)
            {
                ntf.Add(Resx.GetString("_Msg_ЛицоНеДоступно"), NtfStatus.Error);
                return;
            }

            if (!p.IsChecked) ntf.Add(Resx.GetString("_Msg_ЛицоНеПроверено"), NtfStatus.Error);


            var crd = p.GetCard(SchetFactura.Date);
            if (crd != null)
            {
                if (crd.NameLat.Length == 0 && crd.NameRus.Length == 0)
                    ntf.Add(Resx.GetString("_Msg_ЛицоНазвание"), NtfStatus.Error);
            }

            CheckKontrolDataSchByDataOsn(ntf, "3");
        }

        #endregion

        #region ГП

        /// <summary>
        ///  Очистка данных грузополучателя
        /// </summary>
        private void GPPerson_InfoClear()
        {
            SchetFactura.GPPersonDataField.ClearValue();
        }

        /// <summary>
        /// Валидация грузополучателя
        /// </summary>
        private void GpPersonOnRenderNtf(object sender, Ntf ntf)
        {
            ntf.Clear();
            if (string.IsNullOrEmpty(((V4Control)sender).Value)) return;

            if (SchetFactura.GPPersonField.IsValueEmpty)
            {
                GPPerson.ValueText = "";
                return;
            }

            var p = SchetFactura.GPPerson;
            if (p.Unavailable)
            {
                ntf.Add(Resx.GetString("_Msg_ЛицоНеДоступно"), NtfStatus.Error);
                return;
            }

            if (!p.IsChecked) ntf.Add(Resx.GetString("_Msg_ЛицоНеПроверено"), NtfStatus.Error);

            var crd = p.GetCard(SchetFactura.Date);
            if (crd != null)
            {
                if (crd.NameLat.Length == 0 && crd.NameRus.Length == 0)
                    ntf.Add(Resx.GetString("_Msg_ЛицоНазвание"), NtfStatus.Error);
            }
            CheckKontrolDataSchByDataOsn(ntf, "4");
        }

        #endregion

        #region Предоплата

        private void Schet_InfoFullClear()
        {
            SchetFactura.SchetField.ClearValue();

            V3CS_RefreshSchetData();
            Platezhki_InfoFullClear();
        }

        private void Schet_InfoSet(string id)
        {
            Schet_InfoSet(id, "");
        }

        private void Schet_InfoSet(string _id, string _plId)
        {
            SchetFactura.AddBaseDoc(_id, SchetFactura.SchetField.DocFieldId);
            if (_plId.Length == 0) SetPlatezhkiBySchet(_id, true);
            Schet_InfoFullSet(_id);
            Schet.Value = "";
            Schet.ValueText = "";

            V3CS_RefreshSchetData();
        }

        private void Schet_InfoFullSet(string _id)
        {
            if (_id.Length == 0) return;

            var col = SchetFactura.Schets;
            if (col.Count != 1) return;

            Predoplata s = new Predoplata(_id);
            if (s.Unavailable) return;

            if (!s.DataUnavailable)
            {
                //устанавливаем параметры, если они не установлены
                bool f;
                if (SchetFactura.ProdavetsField.IsValueEmpty)
                {
                    f = SchetFactura.PokupatelField.Equals(s.ProdavetsField);
                    SchetFactura.ProdavetsField.Value = (f) ? s.PokupatelField.Value : s.ProdavetsField.Value;
                    SchetFactura.ProdavetsNameField.Value = (f) ? s.PokupatelNameField.Value : s.ProdavetsNameField.Value;
                    SchetFactura.ProdavetsINNField.Value = (f) ? s.PokupatelINNField.Value : s.ProdavetsINNField.Value;
                    SchetFactura.ProdavetsKPPField.Value = (f) ? s.PokupatelKPPField.Value : s.ProdavetsKPPField.Value;
                    SchetFactura.ProdavetsAddressField.Value = (f) ? s.PokupatelAddressField.Value : s.ProdavetsAddressField.Value;


                    if (!f)
                    {
                        SchetFactura.RukovoditelTextField.Value = s.RukovoditelTextField.Value;
                        SchetFactura.BuhgalterTextField.Value = s.BuhgalterTextField.Value;

                        Rukovoditel.ValueText = SchetFactura.RukovoditelTextField.ValueString;
                        Buhgalter.ValueText = SchetFactura.BuhgalterTextField.ValueString;
                    }
                    ShowDetails_Required("1");
                }
                if (SchetFactura.PokupatelField.IsValueEmpty)
                {
                    f = (SchetFactura.ProdavetsField.Equals(s.PokupatelField));

                    SchetFactura.PokupatelField.Value = (f) ? s.ProdavetsField.Value : s.PokupatelField.Value;
                    SchetFactura.PokupatelNameField.Value = (f) ? s.ProdavetsNameField.Value : s.PokupatelNameField.Value;
                    SchetFactura.PokupatelINNField.Value = (f) ? s.ProdavetsINNField.Value : s.PokupatelINNField.Value;
                    SchetFactura.PokupatelKPPField.Value = (f) ? s.ProdavetsKPPField.Value : s.PokupatelKPPField.Value;
                    SchetFactura.PokupatelAddressField.Value = (f) ? s.ProdavetsAddressField.Value : s.PokupatelAddressField.Value;


                    ShowDetails_Required("2");
                }
                if (SchetFactura.DogovorTextField.IsValueEmpty) SchetFactura.DogovorTextField.Value = s.DogovorTextField.Value;
                if (SchetFactura._Dogovor.Length == 0) SchetFactura._Dogovor = s._Dogovor;
                if (SchetFactura._Prilozhenie.Length == 0) SchetFactura._Prilozhenie = s._Prilozhenie;
                if (SchetFactura.CurrencyField.IsValueEmpty) SchetFactura.CurrencyField.Value = s.CurrencyField.Value;
            }
        }

        #endregion

        #region ДокументОснование
        /// <summary>
        /// Событие изменения документа основания
        /// </summary>
        protected void DocumentOsnovanie_Changed(object sender, ProperyChangedEventArgs e)
        {
            if (e.NewValue == null)
            {
                if (EntityId.IsNullEmptyOrZero())
                    JS.Write("if (confirm('{0}')) cmd('cmd', 'ClearAllDoc'); else cmd('cmd', 'ReturnOldValue','DocId','{1}');", Resx.GetString("ClearAllDoc"), e.OldValue);
                else
                    JS.Write("if (confirm('{0}')) cmd('cmd','DocumentOsnovanie_InfoClear','DocId','{1}');", Resx.GetString("msgOsnAttention7"), e.OldValue);

                return;
            }

            var newVal = e.NewValue.ToInt();
            // если уже подвязан документ, но не по полю
            if (Doc.BaseDocs.Exists(d => d.BaseDocId == newVal && d.DocFieldId == null))
            {
                Doc.BaseDocs.RemoveAll(d => d.BaseDocId == newVal && d.DocFieldId == null);
                Doc.AddBaseDoc(e.NewValue, SchetFactura.OsnovanieField.DocFieldId);
            }
               
            var sb = new StringBuilder();
            sb.Append(Resx.GetString("msgOsnAttention0"));
            sb.Append("\\n");
            if (e.NewValue.Length > 0)
            { 
                sb.Append(Resx.GetString("msgOsnAttention2Factura") + " " + DocumentOsnovanie.ValueText + "!\\n");
                sb.Append(Resx.GetString("msgOsnAttention2"));
                sb.Append("\\n");
                sb.Append(Resx.GetString("msgOsnAttention3"));
                sb.Append("\\n");

                sb.Append(Resx.GetString("msgOsnAttention4Factura"));
                sb.Append("\\n");
                if (e.OldValue.Length > 0)
                    sb.Append(Resx.GetString("msgOsnAttention5") + "\\n");
                sb.Append(Resx.GetString("msgOsnAttention6"));
                sb.Append("\\n");
            }
            else
            {
                 sb.Append(Resx.GetString("msgOsnAttention7"));
                 sb.Append("\\n");
            }

            JS.Write("if (confirm('{0}')) cmd('cmd', 'DocumentOsnovanie_InfoSet','DocId','{1}'); else cmd('cmd','DocumentOsnovanie_InfoClear','DocId','{1}');", sb, e.OldValue);
        }

        private void ReturnOldValue(string oldValue)
        {
            DocumentOsnovanie.Value = oldValue;
            
            SchetFactura.OsnovanieField.Value = oldValue;

            var doc = new Document(oldValue);
            DocumentOsnovanie.ValueText = doc.FullDocName;
       }

        /// <summary>
        ///  Валидация документа основания
        /// </summary>
        private void DocumentOsnovanieOnRenderNtf(object sender, Ntf ntf)
        {
            ntf.Clear();
            if (string.IsNullOrEmpty(((V4Control)sender).Value)) return;

            if (SchetFactura._Osnovanie.Length == 0)
            {
                DocumentOsnovanie.ValueText = "";
                return;
            }

            var dOsnovanie = SchetFactura.Osnovanie;

            ValidateBaseDocDate(dOsnovanie, ntf);

            if (dOsnovanie.Unavailable)
            {
                ntf.Add(Resx.GetString("_Msg_ДокументНеДоступен"), NtfStatus.Error);
                return;
            }

            if (dOsnovanie.Date > SchetFactura.Date)
                ntf.Add(Resx.GetString("MsgSchetOsnovanieData"), NtfStatus.Error);

            if (dOsnovanie.DataUnavailable)
            {
                ntf.Add(Resx.GetString("_Msg_ДокументНетЭФ"), NtfStatus.Error);
                return;
            }

            if (!dOsnovanie.Signed)
                ntf.Add(Resx.GetString("ntf_DocIsNotSigned"), NtfStatus.Error);

            switch (dOsnovanie.Type)
            {
                case DocTypeEnum.АктВыполненныхРаботУслуг:
                    var act = new AktUsl(dOsnovanie.Id);
                    if (act.IsCorrected)
                        ntf.Add(Resx.GetString("MSG_HasCorrecting"), NtfStatus.Error);
                    break;
                case DocTypeEnum.ТоварноТранспортнаяНакладная:
                    var ttn = new TTN(dOsnovanie.Id);
                    if (ttn.IsCorrected)
                        ntf.Add(Resx.GetString("MSG_HasCorrecting"), NtfStatus.Error);
                    break;
            }
        }

        /// <summary>
        ///  Документ основание событие перед поиском
        /// </summary> 
        private void DocumentOsnovanieOnBeforeSearch(object sender)
        {
            DocumentOsnovanie.Filter.PersonIDs.Clear();
            if (!SchetFactura.ProdavetsField.IsValueEmpty) DocumentOsnovanie.Filter.PersonIDs.Add(SchetFactura.ProdavetsField.ValueString);
            if (!SchetFactura.PokupatelField.IsValueEmpty) DocumentOsnovanie.Filter.PersonIDs.Add(SchetFactura.PokupatelField.ValueString);

            DocumentOsnovanie.Filter.PersonIDs.UseAndOperator = true;

            DocumentOsnovanie.Filter.LinkedDoc.Clear();

            if (!SchetFactura._Dogovor.IsNullEmptyOrZero())
                DocumentOsnovanie.Filter.LinkedDoc.Add(new LinkedDocParam { DocID = SchetFactura._Dogovor, QueryType = LinkedDocsType.AllСonsequences });
        }

        /// <summary>
        /// Возврат старого значения документа основания
        /// </summary>
        /// <param name="oldValue">Id старого документа (или "")</param>
        private void DocumentOsnovanie_InfoClear(string oldValue)
        {
            SchetFactura._Osnovanie = oldValue;
            if (string.IsNullOrEmpty(SchetFactura._Osnovanie))
            {
                JS.Write("setTimeout(function(){{gi('{0}_0').value = '';}}, 100);", DocumentOsnovanie.HtmlID);
            }

            V3CS_RefreshContactButton();
            V3CS_RefreshSales();

            SchetFactura.SupplierField.ClearValue();
            Supplier.RefreshDataBlock();
            Supplier_InfoClear();
            V3CS_RefreshDetailsSupplier(false);

            Prodavets.IsReadOnly = false;
            Pokupatel.IsReadOnly = false;
            Dogovor.IsReadOnly = false;
            Prilozhenie.IsReadOnly = false;
            BillOfLading.IsReadOnly = false;
        }

        /// <summary>
        /// Действия, выполняемые после изменения значения документа основания
        /// </summary>
        /// <param name="oldValue">Id старого документа (или "")</param>
        private void DocumentOsnovanie_InfoSet(string oldValue)
        {
            // В случае смены одного основания на другое выполняется проверка того, что новое будет удовлетворять условиям, предъявляемым к основанию
            var osnId = SchetFactura._Osnovanie;
            if (osnId.Length > 0)
            {
                var d = new Document(osnId);

                if (!DocumentOsnovanie_FacilityChange(d))
                {
                    DocumentOsnovanie_InfoClear(oldValue);
                    return;
                }
            }

            SetSchDataBy_DocumentOsnovanie(osnId);

            V3CS_RefreshSales();
            SetNumberByOsnovanie();
        }

        /// <summary>
        /// Проверка основания (документа реализации) на доступность, наличие ЭФ, отсутствие в основаниях других счетов-фактур
        /// </summary>
        /// <returns>соответствует док-т реализации условиям или нет</returns>
        private bool DocumentOsnovanie_FacilityChange(Document d)
        {
            if (d.Unavailable)
            {
                ShowMessage(Resx.GetString("_Msg_ОснованиеНеДоступно")); // Документ основание не доступен!
                return false;
            }

            if (d.DataUnavailable)
            {
                ShowMessage(Resx.GetString("_Msg_ОснованиеНетФормы")); // Документ основание не имеет электронной формы!
               // return false; // Анисимов хочет возможность добавление без электронной формы
            }
            var sequels = d.GetSequelDocs();

            foreach (var sequelDoc in sequels)
            {
                if (sequelDoc.Type == DocTypeEnum.СчетФактура && sequelDoc.DocId != SchetFactura.DocId)
                {
                    var message = Resx.GetString("_Msg_ОснованиеУжеЕсть1") +" " + sequelDoc.FullDocName + ", " + Resx.GetString("_Msg_ОснованиеУжеЕсть2") +" " + d.FullDocName;
                    JS.Write("alert('{0}');", message);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Проставление номера счету-фактуре по следующему алгоритму:
        /// Если из документа-реализации, на основе которого создается счет-фактура, вытекает инвойс, то:
        ///		1. № счета-фактуры = № инвойса.
        ///		2. Дата документа и номер - ReadOnly.
        /// Иначе:
        ///		1. № счета-фактуры = sp_СозданиеНомераДокумента
        ///		2. Дата документа, номер и контрагент - нередактируемые для документов с созданной ЭФ.
        /// Аналогично у инвойсов!
        /// </summary>
        private void SetNumberByOsnovanie()
        {
            var osnId = SchetFactura._Osnovanie;

            if (osnId.Length > 0)
            {
                var typeId = (int)DocTypeEnum.Инвойс;
                var dtInvoice = Document.LoadSequelDocs(typeId.ToString(), "1145", osnId);

                if (dtInvoice.Rows.Count == 1)
                {
                    InvoiceDocument inv = new InvoiceDocument(dtInvoice.Rows[0][0].ToString());
                    if (!inv.Unavailable)
                    {
                        SchetFactura.Number = inv.Number;
                        NumberReadOnly = true;
                        RefreshNumber();
                        SchetFactura.Date = inv.Date;
                        DocDateReadOnly = true;
                        RefreshrDocNumDateNameRows();
                    }
                }
            }
        }

        #endregion

        #endregion

        #region Swap

        private void ChangeWeakSwap(int inx, StringCollection personDocs)
        {
            if (personDocs.Contains((inx == 0) ? SchetFactura.ProdavetsField.ValueString : SchetFactura.PokupatelField.ValueString))
            {
                foreach (string _pd in personDocs)
                {
                    if (inx == 0)
                    {
                        if (!SchetFactura.ProdavetsField.ValueString.Equals(_pd) && SchetFactura.PokupatelField.IsValueEmpty)
                        {
                            SchetFactura.PokupatelField.Value = _pd;
                            Pokupatel_Changed();
                            break;
                        }
                    }
                    else
                    {
                        if (!SchetFactura.PokupatelField.ValueString.Equals(_pd) && SchetFactura.ProdavetsField.IsValueEmpty)
                        {
                            SchetFactura.ProdavetsField.Value = _pd;
                            //  Prodavets_Changed(); ---------------todo-----------------
                            break;
                        }
                    }
                }
                if (SchetFactura.ProdavetsField.ValueString.Equals(SchetFactura.PokupatelField.ValueString))
                {
                    foreach (string _pd in personDocs)
                    {
                        if (inx == 0)
                        {
                            if (!_pd.Equals(SchetFactura.ProdavetsField.ValueString))
                            {
                                SchetFactura.PokupatelField.Value = _pd;
                                Pokupatel_Changed();
                                break;
                            }
                        }
                        else
                        {
                            if (!_pd.Equals(SchetFactura.PokupatelField.ValueString))
                            {
                                SchetFactura.ProdavetsField.Value = _pd;
                                // Prodavets_Changed();  ---------------todo-----------------
                                break;
                            }
                        }
                    }

                }
                ShowDetails_Required("1");
                ShowDetails_Required("2");
            }
        }

        #endregion

        #region SetDataInfo

        /// <summary>
        ///  Получить значение 
        /// </summary>
        private string SetPerson_DataInfo(PersonOld p)
        {
            if (p == null) return "";

            var crd = p.GetCard(SchetFactura.Date == DateTime.MinValue ? DateTime.Today : SchetFactura.Date);

            if (crd != null)
            {
                return IsRusLocal ? crd.NameRus : crd.NameLat;
            }

            return "";
        }

        #endregion

        #region ShowMOdalDialog

        private void GetContact(string inx, string _p, string dialogResult)
        {
            switch (inx)
            {
                case "1":
                    _p = SchetFactura.ProdavetsField.ValueString;
                    break;
                case "2":
                    _p = SchetFactura.PokupatelField.ValueString;
                    break;
                case "3":
                    _p = SchetFactura.GOPersonField.ValueString;
                    break;
                case "4":
                    _p = SchetFactura.GPPersonField.ValueString;
                    break;
                case "5":
                    _p = SchetFactura.SupplierField.ValueString;
                    break;
            }
            if (_p.Length == 0)
            {
                V3CS_Alert("Для выбора контакта необходимо указать лицо!");
                return;
            }
            if (dialogResult == null)
            {
                JS.Write("cmd('cmd','GetContact','arg0', '{0}', 'arg1','{1}', 'arg2', window.showModalDialog('contact.aspx?personid={1}&inx={0}',null,{2}));",
                         inx,
                         _p,
                         new DialogWindowFeatures(600, 130, true, true));

                return;
            }


            if (dialogResult.Length > 0)
            {
                switch (inx)
                {
                    case "1":
                        SchetFactura.ProdavetsAddressField.Value = dialogResult;
                        Prodavets.RefreshDataBlock();
                        break;
                    case "2":
                        SchetFactura.PokupatelAddressField.Value = dialogResult;
                        Pokupatel.RefreshDataBlock();
                        break;
                    case "3":
                        SchetFactura.GOPersonDataField.Value = SetPerson_DataInfo(SchetFactura.GOPerson);
                        break;
                    case "4":
                        SchetFactura.GPPersonDataField.Value = SetPerson_DataInfo(SchetFactura.GPPerson);
                        break;
                    case "5":
                        SchetFactura.SupplierAddressField.Value = dialogResult;
                        Supplier.RefreshDataBlock();
                        break;
                }
            }
        }

        #endregion

        #region Render

        /// <summary>
        ///  Обновить таблицу продажи
        /// </summary>
        private void V3CS_RefreshSales()
        {
            StringWriter w = new StringWriter();
            RenderSales(w);
            JS.Write("gi('Sales').innerHTML={0};", HttpUtility.JavaScriptStringEncode(w.ToString(), true));
        }

        /// <summary>
        ///  Рендер позиции по документу основанию
        /// </summary>
        protected void RenderSales(TextWriter w)
        {
            if (SchetFactura._Osnovanie.Length == 0)
            {
                w.Write("<i>&nbsp;отсутствуют</i>");
                return;
            }

            Document d = new Document(SchetFactura._Osnovanie);
            if (d.Unavailable || d.DocumentData.Unavailable)
            {
                w.Write("<i>&nbsp;не доступны</i>");
                return;
            }

            switch (d.Type)
            {
                case DocTypeEnum.ТоварноТранспортнаяНакладная:
                    RenderTTNPositions(w, d.Id);
                    break;
                case DocTypeEnum.АктВыполненныхРаботУслуг:
                   RenderAktUslPositions(w, d.Id);
                    break;
                case DocTypeEnum.Претензия:
                    RenderClaimPositions(w, d.Id);
                    break;
                default:
                    w.Write("<i>не реализовано</i>");
                    break;
            }
        }

        /// <summary>
        ///  Рендер позиции по документу основанию - ТТН
        /// </summary>
        protected void RenderTTNPositions(TextWriter w, string id)
        {
            TTN nkl = new TTN(id);

            DataTable tbl = TTN.GetMrisSales(id);
            DataTable tblUsl = AktUsl.GetUslsGroup(id);

            w.Write(@"
			<table width=""100%"" cellpadding=""0"" cellspacing=""0"" class=""grid"">
			<tr class=""gridHeader"">
				<td align=""center"" >Продукт/Услуга</td>
				<td align=""center"" >Кол-во</td>
				<td align=""center"" >Ед.</td>
				<td align=""center"" >Цена без НДС</td>
				<td align=""center"" >%</td>
				<td align=""center"" >Сумма без НДС</td>
				<td align=""center"" >НДС</td>
				<td align=""center"" >Всего</td>
			</tr>");

            NumberFormatInfo nfiDouble = (NumberFormatInfo)NumberFormatInfo.CurrentInfo.Clone();
            nfiDouble.CurrencySymbol = "";
            nfiDouble.CurrencyDecimalDigits = 0;

            bool UE;
            CheckFieldDogovorUE(out UE);

            int minScale = nkl.Currency.UnitScale, maxScale = 4;

            #region MRIS

            RenderTableUsl(w, tbl, nkl.CurrencyField.IsValueEmpty ? null : nkl.Currency);

            //Итого
            w.Write("<tr>");
            w.Write("<td colspan='5' align='right'><i>Итого по продуктам</i>:</td>");
            w.Write("<td align='right' nowrap>");
            RenderNumber(w, nkl.SummaOutNDSAll_Mris.ToString(), minScale, maxScale, " ");

            w.Write("</td>");
            w.Write("<td  align='right' nowrap>");
            RenderNumber(w, nkl.SummaNDSAll_Mris.ToString(), minScale, maxScale, " ");

            w.Write("</td>");
            w.Write("<td  align='right' nowrap>");
            RenderNumber(w, nkl.VsegoAll_Mris.ToString(), minScale, maxScale, " ");

            w.Write("</td>");
            w.Write("</tr>");

            #endregion

            #region USLS

            RenderTableUsl(w, tblUsl, nkl.CurrencyField.IsValueEmpty ? null : nkl.Currency);

            //Итого

            w.Write("<tr>");
            w.Write("<td colspan='5' align='right'><i>Итого по оказанным услугам</i>:</td>");
            w.Write("<td  align='right' nowrap>");
            RenderNumber(w, nkl.SummaOutNDSAll_USL.ToString(), minScale, maxScale, " ");

            w.Write("</td>");
            w.Write("<td  align='right' nowrap>");
            RenderNumber(w, nkl.SummaNDSAll_USL.ToString(), minScale, maxScale, " ");

            w.Write("</td>");
            w.Write("<td  align='right' nowrap>");
            RenderNumber(w, nkl.VsegoAll_USL.ToString(), minScale, maxScale, " ");

            w.Write("</td>");
            w.Write("</tr>");

            #endregion

            #region Итого

            w.Write("<tr>");
            w.Write("<td colspan='5' align='right'>Всего по счету:</td>");
            w.Write("<td  align='right' nowrap>");
            RenderNumber(w, nkl.SummaOutNDSAll.ToString(), minScale, maxScale, " ");
            if (!nkl.CurrencyField.IsValueEmpty)
            {
                RenderLinkResource(w, nkl.CurrencyField.ValueString);
                w.Write(HttpUtility.HtmlEncode(IsRusLocal ? nkl.Currency.UnitRus : nkl.Currency.UnitEng));
                RenderLinkEnd(w);
            }
            else if (UE)
                w.Write("у.е.");

            w.Write("</td>");
            w.Write("<td align='right' nowrap>");
            RenderNumber(w, nkl.SummaNDSAll.ToString(), minScale, maxScale, " ");
            if (!nkl.CurrencyField.IsValueEmpty)
            {
                RenderLinkResource(w, nkl.CurrencyField.ValueString);
                w.Write(HttpUtility.HtmlEncode(IsRusLocal ? nkl.Currency.UnitRus : nkl.Currency.UnitEng));
                RenderLinkEnd(w);
            }
            else if (UE)
                w.Write("у.е.");

            w.Write("</td>");
            w.Write("<td align='right' nowrap>");
            w.Write(RenderVsegoMoney1(nkl.VsegoAll));

            if (!nkl.CurrencyField.IsValueEmpty)
            {
                RenderLinkResource(w, nkl.CurrencyField.ValueString);
                w.Write(HttpUtility.HtmlEncode(IsRusLocal ? nkl.Currency.UnitRus : nkl.Currency.UnitEng));
                RenderLinkEnd(w);
            }
            else if (UE)
                w.Write("у.е.");
            w.Write("</td>");
            w.Write("</tr>");

            #endregion

            w.Write("</table><br>");
        }

        /// <summary>
        ///  Рендер позиции по документу основанию - Акт выполненых работ и услуг
        /// </summary>
        protected void RenderAktUslPositions(TextWriter w, string id)
        {
            AktUsl akt = new AktUsl(id);
            DataTable tbl = AktUsl.GetUslsGroup(id);

            w.Write(@"
			<table width=""70%"" cellpadding=""0"" cellspacing=""0"" class=""grid"">
			<tr class=""gridHeader"">
				<td align=""center"" >Продукт/Услуга</td>
				<td align=""center"" >Кол-во</td>
				<td align=""center"" >Ед.</td>
				<td align=""center"" >Цена без НДС</td>
				<td align=""center"" >%</td>
				<td align=""center"" >Сумма без НДС</td>
				<td align=""center"" >НДС</td>
				<td align=""center"" >Всего</td>
			</tr>");

            NumberFormatInfo nfiDouble = (NumberFormatInfo)NumberFormatInfo.CurrentInfo.Clone();
            nfiDouble.CurrencySymbol = "";
            nfiDouble.CurrencyDecimalDigits = 0;

            int minScale = akt.Currency.UnitScale, maxScale = 4;

            #region USLS

            bool UE;
            CheckFieldDogovorUE(out UE);

            RenderTableUsl(w, tbl, akt.CurrencyField.IsValueEmpty ? null : akt.Currency);

            //Итого
            w.Write("<tr>");
            w.Write("<td colspan='5' align='right'><i>Итого по оказанным услугам</i>:</td>");
            w.Write("<td  align='right' nowrap>");
            RenderNumber(w, akt.SummaOutNDSAll.ToString(), minScale, maxScale, " ");

            w.Write("</td>");
            w.Write("<td  align='right' nowrap>");
            RenderNumber(w, akt.SummaNDSAll.ToString(), minScale, maxScale, " ");

            w.Write("</td>");
            w.Write("<td  align='right' nowrap>");
            RenderNumber(w, akt.VsegoAll.ToString(), minScale, maxScale, " ");

            w.Write("</td>");
            w.Write("</tr>");

            #endregion

            #region Итого


            w.Write("<tr>");
            w.Write("<td colspan='5' align='right'>Всего по счету:</td>");
            w.Write("<td align='right' nowrap>");
            RenderNumber(w, akt.SummaOutNDSAll.ToString(), minScale, maxScale, " ");


            if (!akt.CurrencyField.IsValueEmpty && !UE)
            {
                RenderLinkResource(w, akt.CurrencyField.ValueString);
                w.Write(HttpUtility.HtmlEncode(IsRusLocal ? akt.Currency.UnitRus : akt.Currency.UnitEng));
                RenderLinkEnd(w);
            }
            else if (UE)
                w.Write("у.е.");

            w.Write("</td>");
            w.Write("<td align='right' nowrap>");
            RenderNumber(w, akt.SummaNDSAll.ToString(), minScale, maxScale, " ");
            if (!akt.CurrencyField.IsValueEmpty && !UE)
            {
                RenderLinkResource(w, akt.CurrencyField.ValueString);
                w.Write(HttpUtility.HtmlEncode(IsRusLocal ? akt.Currency.UnitRus : akt.Currency.UnitEng));
                RenderLinkEnd(w);
            }
            else if (UE)
                w.Write("у.е.");

            w.Write("</td>");
            w.Write("<td align='right' nowrap>");
            w.Write(RenderVsegoMoney1(akt.VsegoAll));

            if (!akt.CurrencyField.IsValueEmpty && !UE)
            {
                RenderLinkResource(w, akt.CurrencyField.ValueString);
                w.Write(HttpUtility.HtmlEncode(IsRusLocal ? akt.Currency.UnitRus : akt.Currency.UnitEng));
                RenderLinkEnd(w);
            }
            else if (UE)
                w.Write("у.е.");

            w.Write("</td>");
            w.Write("</tr>");

            #endregion

            w.Write("</table><br>");
        }

        /// <summary>
        ///  Рендер позиции по документу основанию - Претензия
        /// </summary>
        protected void RenderClaimPositions(TextWriter w, string id)
        {
            var clm = new Claim(id);
            var positions = Claim.Position.GetPositionsByClaimId(id.ToInt());

            w.Write(@"
			<table width=""70%"" cellpadding=""0"" cellspacing=""0"" class=""grid"">
			<tr class=""gridHeader"">
				<td align=""center"">Претензия</td>
				<td align=""center"">Кол-во</td>
				<td align=""center"">Ед.</td>
				<td align=""center"">Цена</td>
				<td align=""center"">Сумма</td>
			</tr>");

            NumberFormatInfo nfiDouble = (NumberFormatInfo)NumberFormatInfo.CurrentInfo.Clone();
            nfiDouble.CurrencySymbol = "";
            nfiDouble.CurrencyDecimalDigits = 0;

            int minScale = clm.Currency.UnitScale, maxScale = 4;

            #region USLS

            bool UE;
            CheckFieldDogovorUE(out UE);
            foreach (Claim.Position pos in positions)
            {
                if (pos.ResourceId != 0)
                //    nfiDouble.CurrencyDecimalDigits = pos.Resource.GetScale4Unit(pos._Unit, 0);

                w.Write("<tr>");

                //Ресурс
                w.Write("<td>");
                if (pos.ResourceId > 0)
                {
                    RenderLinkResource(w, pos.ResourceId.ToString());
                    w.Write(pos.ResourceText);
                    RenderLinkEnd(w);
                }
                w.Write("</td>");

                //Количество и Ед.Измерения 
                w.Write("<td align='right' nowrap>{0}</td><td align='center'>{1}</td>",
                    pos.Quantity == 0.0 ? "" : pos.Quantity.ToString("C", nfiDouble),
                    pos.UnitId == 0 ? "&nbsp;" : pos.Unit.Name
                    );

                //Цена без НДС
                w.Write("<td align='right' nowrap>");
                RenderNumber(w, pos.CostOutNds.ToString("N2"), minScale, maxScale, " ");
                w.Write("</td>");

                //Сумма без НДС
                w.Write("<td align='right' nowrap>");
                RenderNumber(w, pos.SummaOutNds.ToString("N2"), minScale, maxScale, " ");
                w.Write("</td>");

                w.Write("</tr>");
            }

            //Итого
            w.Write("<tr>");
            w.Write("<td colspan='4' align='right'><i>Итого по притензиям</i>:</td>");
            w.Write("<td align='right' nowrap>");

            decimal all = positions.Sum(p => p.SummaOutNds);

            w.Write(RenderVsegoMoney1(all)); 

            w.Write("</td>");
            w.Write("</tr>");

            #endregion

            w.Write("</table><br>");
        }

        /// <summary>
        ///  Обновить данные платежек
        /// </summary>
        protected void V3CS_RefreshPlatezhkiData()
        {
            Platezhki.SelectedItems.Clear();
            Platezhki.SelectedItems.AddRange(Doc.GetDocLinksItems(SchetFactura.PlatezhkiField.DocFieldId));
            //using (var w = new StringWriter())
            //{
            //    RenderPlatezhkiData(w);
            //    JS.Write("gi('PlatezhkiData').innerHTML={0};", HttpUtility.JavaScriptStringEncode(w.ToString(), true));
            //}
        }

        ///// <summary>
        /////  Отрисовать данные платежек
        ///// </summary>
        ///// <param name="w"></param>
        //protected void RenderPlatezhkiData(TextWriter w)
        //{
        //    RenderBaseDocsHTML(w, SchetFactura.PlatezhkiField);
        //}
        protected void RenderCorrectingSequel(TextWriter w)
        {
            if (SchetFactura.IsCorrected)
            {
                w.Write(Resx.GetString("MSG_CorrectedWith"));
                RenderLinkDoc(w, SchetFactura._CorrectingSequelDoc);
                w.Write(SchetFactura.CorrectingSequelDoc.FullDocName);
                RenderLinkEnd(w);
            }
        }

        /// <summary>
        ///  Обновить данные счетов
        /// </summary>
        protected void V3CS_RefreshSchetData()
        {
            //using (var w = new StringWriter())
            //{
            //    RenderSchetData(w);
            //    JS.Write("gi('SchetData').innerHTML={0};", HttpUtility.JavaScriptStringEncode(w.ToString(), true));
            //}
            Schet.SelectedItems.Clear();
            Schet.SelectedItems.AddRange(Doc.GetDocLinksItems(SchetFactura.SchetField.DocFieldId));
        }

        /// <summary>
        ///  Обновить данные контактов
        /// </summary>
        private void V3CS_RefreshContactButton()
        {
            V3CS_RefreshContactButton1();
            V3CS_RefreshContactButton2();
            V3CS_RefreshContactButton3();
            V3CS_RefreshContactButton4();
            V3CS_RefreshContactButton5();
        }

        /// <summary>
        ///  Обновить адрес продавца
        /// </summary>
        protected void V3CS_RefreshContactButton1()
        {
            using (var w = new StringWriter())
            {
                RenderContactButton1(w);
                JS.Write("gi('Contact1').innerHTML={0};", HttpUtility.JavaScriptStringEncode(w.ToString(), true));
            }
        }

        /// <summary>
        ///  Обновить адрес покупателя
        /// </summary>
        protected void V3CS_RefreshContactButton2()
        {
            using (var w = new StringWriter())
            {
                RenderContactButton2(w);
                JS.Write("gi('Contact2').innerHTML={0};", HttpUtility.JavaScriptStringEncode(w.ToString(), true));
            }
        }

        /// <summary>
        ///  Обновить контакты грузоотправителя
        /// </summary>
        protected void V3CS_RefreshContactButton3()
        {
            using (var w = new StringWriter())
            {
                RenderContactButton3(w);
                JS.Write("gi('Contact3').innerHTML={0};", HttpUtility.JavaScriptStringEncode(w.ToString(), true));
            }
        }

        /// <summary>
        ///  Обновить контакты грузополучателя
        /// </summary>
        protected void V3CS_RefreshContactButton4()
        {
            using (var w = new StringWriter())
            {
                RenderContactButton4(w);
                JS.Write("gi('Contact4').innerHTML={0};", HttpUtility.JavaScriptStringEncode(w.ToString(), true));
            }
        }

        /// <summary>
        ///  Обновить адрес поставщика
        /// </summary>
        protected void V3CS_RefreshContactButton5()
        {
            using (var w = new StringWriter())
            {
                RenderContactButton5(w);
                JS.Write("gi('Contact5').innerHTML={0};", HttpUtility.JavaScriptStringEncode(w.ToString(), true));
            }
        }

        /// <summary>
        ///  Отрисовать адрес продавца
        /// </summary>
        protected void RenderContactButton1(TextWriter w)
        {
            if (!IsPrintVersion && DocEditable && (SchetFactura._Osnovanie.Length == 0 || SchetFactura.ProdavetsAddressField.IsValueEmpty))
                w.Write("<a href=\"#\" onclick=\"cmd('cmd','GetContact', 'arg0', '1');\"><img border=\"0\" src=\"/styles/contact.gif\" alt=\"выбрать адрес отличный от юридического\"></a>");
        }

        /// <summary>
        ///  Отрисовать адрес покупателя
        /// </summary>
        protected void RenderContactButton2(TextWriter w)
        {
            if (!IsPrintVersion && DocEditable && (SchetFactura._Osnovanie.Length == 0 || SchetFactura.PokupatelAddressField.IsValueEmpty))
                w.Write("<a href=\"#\" onclick=\"cmd('cmd','GetContact', 'arg0','2');\"><img border=\"0\" src=\"/styles/contact.gif\" alt=\"выбрать адрес отличный от юридического\"></a>");
        }

        /// <summary>
        ///  Отрисовать контакты грузоотправителя
        /// </summary>
        protected void RenderContactButton3(TextWriter w)
        {
            if (!IsPrintVersion && DocEditable && (SchetFactura._Osnovanie.Length == 0 || SchetFactura.GOPersonDataField.IsValueEmpty))
                w.Write("<a href=\"#\" onclick=\"cmd('cmd','GetContact', 'arg0','3');\"><img border=\"0\" src=\"/styles/contact.gif\" alt=\"выбрать адрес отправки груза\"></a>");
        }

        /// <summary>
        ///  Отрисовать контакты грузополучателя
        /// </summary>
        protected void RenderContactButton4(TextWriter w)
        {
            if (!IsPrintVersion && DocEditable && (SchetFactura._Osnovanie.Length == 0 || SchetFactura.GPPersonDataField.IsValueEmpty))
                w.Write("<a href=\"#\" onclick=\"cmd('cmd','GetContact', 'arg0','4');\"><img border=\"0\" src=\"/styles/contact.gif\" alt=\"выбрать адрес отправки груза\"></a>");
        }

        /// <summary>
        ///  Отрисовать адрес поставщика
        /// </summary>
        protected void RenderContactButton5(TextWriter w)
        {
            if (!IsPrintVersion && DocEditable && (SchetFactura._Osnovanie.Length == 0 || SchetFactura.SupplierAddressField.IsValueEmpty))
                w.Write("<a href=\"#\" onclick=\"cmd('cmd','GetContact', 'arg0','5');\"><img border=\"0\" src=\"/styles/contact.gif\" alt=\"выбрать адрес отличный от юридического\"></a>");
        }

        ///// <summary>
        /////  Отрисовать данные счетов
        ///// </summary>
        //protected void RenderSchetData(TextWriter w)
        //{
        //    RenderBaseDocsHTML(w, SchetFactura.SchetField);
        //}

        /// <summary>
        ///  Обновить курс
        /// </summary>
        private void V3CS_RefreshKurs()
        {
            using (var w = new StringWriter())
            {
                RenderKurs(w);
                JS.Write("gi('spKurs').innerHTML={0};", HttpUtility.JavaScriptStringEncode(w.ToString(), true));
            }
        }

        /// <summary>
        ///  Отрисовать курс
        /// </summary>
        protected void RenderKurs(TextWriter w)
        {
            if (SchetFactura.KursField.IsValueEmpty)
            {
                w.Write("");
                return;
            }

            w.Write("&nbsp;1 у.е. = ");
            RenderNumber(w, SchetFactura.KursField.ValueString, 0, 8, " ");
            if (!SchetFactura.CurrencyField.IsValueEmpty && !SchetFactura.Currency.Unavailable) w.Write(SchetFactura.Currency.UnitRus);

            if (SchetFactura.Date > DateTime.MinValue)
                w.Write(" (на " + SchetFactura.Date.ToString("dd.MM.yy") + ")");

        }

        /// <summary>
        ///  Отрисовать куратора договора
        /// </summary>
        private void V3CS_RefreshDogovorKurator()
        {
            using (var w = new StringWriter())
            {
                RenderDogovorKurator(w);
                JS.Write("gi('divKurator').innerHTML={0};", HttpUtility.JavaScriptStringEncode(w.ToString(), true));
            }
        }

        private void V3CS_RefreshFacturaPril()
        {
            using (var w = new StringWriter())
            {
                RenderFacturaPril(w);
                JS.Write("gi('FacturaPril').innerHTML={0};", HttpUtility.JavaScriptStringEncode(w.ToString(), true));
            }
        }

        /// <summary>
        ///  Отрисовка приложения
        /// </summary>
        protected void RenderFacturaPril(TextWriter w)
        {
            if (ShowFacturaPril)
            {
                DataTable tblSch = SchetFactura.GetFacturaPril();
                if (tblSch.Rows.Count == 0)
                {
                    w.Write("&nbsp;В документе основании нет информации об отправках");
                    return;
                }

                Table tbl = new Table();
                TableRow row = new TableRow();

                tbl.CssClass = "grid";
                tbl.CellPadding = 0;
                tbl.CellSpacing = 0;

                row.Cells.Add(new TableCell { Text = "Дата открузки" });
                row.Cells.Add(new TableCell { Text = "Накладная" });
                row.Cells.Add(new TableCell { Text = "Вагон" });
                row.Cells.Add(new TableCell { Text = "Количество" });
                row.Cells.Add(new TableCell { Text = "Ед. изм" });
                row.Cells.Add(new TableCell { Text = "Количество" });

                row.CssClass = "gridHeader";
                tbl.Rows.Add(row);

                int ei = 0;
                string eiText = "";
                double sum = 0;

                DataRow[] rowsSch = tblSch.Select("", "КодЕдиницыИзмерения");
                if (rowsSch.Length > 0)
                {
                    ei = Convert.IsDBNull(rowsSch[0]["КодЕдиницыИзмерения"]) ? 0 : (int)rowsSch[0]["КодЕдиницыИзмерения"];
                    eiText = Convert.IsDBNull(rowsSch[0]["Единица"]) ? "" : rowsSch[0]["Единица"].ToString();
                }

                foreach (DataRow rowSch in rowsSch)
                {
                    TableCell cell;

                    int eiNew = Convert.IsDBNull(rowSch["КодЕдиницыИзмерения"]) ? 0 : (int)rowSch["КодЕдиницыИзмерения"];
                    if (ei != eiNew)
                    {
                        TableRow rowItogo = new TableRow();

                        cell = new TableCell();
                        cell.ColumnSpan = 3;
                        cell.HorizontalAlign = HorizontalAlign.Right;
                        cell.Font.Bold = true;
                        cell.Text = "Итого:&nbsp;";
                        rowItogo.Cells.Add(cell);

                        cell = new TableCell();
                        cell.HorizontalAlign = HorizontalAlign.Right;
                        cell.Font.Bold = true;
                        cell.Text = sum.ToString("N3");
                        rowItogo.Cells.Add(cell);

                        cell = new TableCell();
                        cell.HorizontalAlign = HorizontalAlign.Center;
                        cell.Font.Bold = true;
                        cell.Text = eiText;
                        rowItogo.Cells.Add(cell);

                        tbl.Rows.Add(rowItogo);
                        sum = 0;
                        ei = eiNew;
                    }

                    //Дата открузки
                    row = new TableRow();

                    cell = new TableCell();
                    cell.Text = Convert.IsDBNull(rowSch["ДатаОтгрузки"]) ? "" : ((DateTime)rowSch["ДатаОтгрузки"]).ToString("dd.MM.yyyy");
                    cell.HorizontalAlign = HorizontalAlign.Center;
                    row.Cells.Add(cell);

                    //Накладная
                    cell = new TableCell();
                    cell.Text = Convert.IsDBNull(rowSch["Накладная"]) ? "" : rowSch["Накладная"].ToString();
                    cell.HorizontalAlign = HorizontalAlign.Center;
                    row.Cells.Add(cell);

                    //Вагон
                    cell = new TableCell();
                    cell.Text = Convert.IsDBNull(rowSch["Вагон"]) ? "" : rowSch["Вагон"].ToString();
                    cell.HorizontalAlign = HorizontalAlign.Center;
                    row.Cells.Add(cell);

                    //Количество
                    cell = new TableCell();
                    cell.Text = Convert.IsDBNull(rowSch["Количество"]) ? "" : ((double)rowSch["Количество"]).ToString("N3");
                    cell.HorizontalAlign = HorizontalAlign.Right;
                    row.Cells.Add(cell);

                    //Ед. изм
                    cell = new TableCell();
                    eiText = cell.Text = Convert.IsDBNull(rowSch["Единица"]) ? "" : rowSch["Единица"].ToString();
                    cell.HorizontalAlign = HorizontalAlign.Center;
                    row.Cells.Add(cell);

                    tbl.Rows.Add(row);

                    sum += (double)rowSch["Количество"];
                }

                if (rowsSch.Length > 0)
                {
                    TableRow rowItogo = new TableRow();
                    TableCell cell;

                    cell = new TableCell();
                    cell.ColumnSpan = 3;
                    cell.HorizontalAlign = HorizontalAlign.Right;
                    cell.Font.Bold = true;
                    cell.Text = "Итого:&nbsp;";
                    rowItogo.Cells.Add(cell);

                    cell = new TableCell();
                    cell.HorizontalAlign = HorizontalAlign.Right;
                    cell.Font.Bold = true;
                    cell.Text = sum.ToString("N3");
                    rowItogo.Cells.Add(cell);

                    cell = new TableCell();
                    cell.HorizontalAlign = HorizontalAlign.Center;
                    cell.Font.Bold = true;
                    cell.Text = eiText;
                    rowItogo.Cells.Add(cell);

                    tbl.Rows.Add(rowItogo);
                }

                HtmlTextWriter wr = new HtmlTextWriter(w);
                tbl.RenderControl(wr);
            }
            else
            {
                w.Write("");
            }
        }

        /// <summary>
        ///  Отрисовать куратора договора
        /// </summary>
        protected void RenderDogovorKurator(TextWriter w)
        {
            Dogovor dog = null;
            if (SchetFactura.DogovorField.IsValueEmpty)
                dog = SchetFactura.Dogovor;

            if (dog == null || dog.Unavailable || dog.DataUnavailable) return;

            if (dog._Kurator.Length == 0) return;
            if (dog.Kurator.Unavailable) return;
            w.Write("<nobr>Куратор:");
            RenderLinkEmployee(w, "kuratorLinkId", dog.Kurator, NtfStatus.Empty);

            w.Write("</nobr>");
        }

        /// <summary>
        ///  Обновить отрисовку куратора договора
        /// </summary>
        protected void RefreshDogovorKurator()
        {
            using (var w = new StringWriter())
            {
                RenderDogovorKurator(w);
                JS.Write("gi('divKurator').innerHTML={0};", HttpUtility.JavaScriptStringEncode(w.ToString(), true));
            }
        }

        #endregion

        #region Работа с платежками и счетами на предоплату

        private void SetPlatezhkiBySchet(string _id, bool fl)
        {
            DSODocument dso = new DSODocument();

           // dso.Type.Set(2066,FOptItemFlags.Equals|FOptItemFlags.ChildOf|FOptItemFlags.SameAs);
            dso.Type.DocTypeParams.Add(new DocTypeParam { DocTypeEnum = DocTypeEnum.ПлатежноеПоручение, QueryType = DocTypeQueryType.Equals });
            dso.Type.DocTypeParams.Add(new DocTypeParam { DocTypeEnum = DocTypeEnum.Swift, QueryType = DocTypeQueryType.Equals });
            // dso.Type.Enabled = true;

            // dso.LinkedDoc.LinkedDocParams.Add(new LinkedDocParam{DocID = _id, QueryType = LinkedDocsType.DirectСonsequences });
            // //dso.LinkedDoc.Enabled = true;

            // var col = new StringCollection();
            // col = Kesco.Lib.Web.ConvertExtention.Convert.Str2Collection(sch._Platezhki);
            // foreach (DataRow dr in dso.GetData().Rows)
            //     if (!col.Contains(dr[0].ToString())) col.Add(dr[0].ToString());


            // sch._Platezhki = Kesco.Lib.Web.ConvertExtention.Convert.Collection2Str(col);

            // if (fl) V3CS_RefreshPlatezhkiData();
        }

        private void SetSchetByPlatezhki(string id, bool onlyId)
        {
            //Платежки
            var d = new Document(id);
            //Коллекция Всех документов оснований платежек
            var colPl2 = d.GetBaseDocsAll();

            //Коллекция Всех счетов
            var colSch2 = SchetFactura.Schets.ToList();

            bool fl = false;

            foreach (var dOsn in colPl2)
            {
                if (dOsn.Type == DocTypeEnum.Счет && !colSch2.Exists(s => s.Id == dOsn.Id))
                {
                    colSch2.Add(dOsn);

                    SchetFactura.Schets = colSch2;
                    if (colSch2.Count == 1)
                    {
                        if (!onlyId) Schet_InfoSet(dOsn.Id);
                        else Schet_InfoSet(dOsn.Id, id);
                    }
                    if (!onlyId) SetPlatezhkiBySchet(dOsn.Id, false);
                    fl = true;
                }
            }

            if (fl)
            {
                V3CS_RefreshSchetData();
                V3CS_RefreshPlatezhkiData();
            }
        }

        #endregion

        #region Работа с документом основанием

        /// <summary>
        ///  Установить на основании счета фактуры
        /// </summary>
        private void SetSchDataBy_Sch(string _id)
        {
            SchetFactura fak = new SchetFactura(_id);

            SchetFactura.CurrencyField.Value = fak.CurrencyField.Value;

            //Реальный поставщик
            Supplier_InfoClear();
            SchetFactura.SupplierField.Value = fak.SupplierField.Value;
            SchetFactura.SupplierINNField.Value = fak.SupplierINNField.Value;
            SchetFactura.SupplierAddressField.Value = fak.SupplierAddressField.Value;
            SchetFactura.SupplierKPPField.Value = fak.SupplierKPPField.Value;
            SchetFactura.SupplierNameField.Value = fak.SupplierNameField.Value;
            ShowDetails_Required("5");

            SchetFactura.ProdavetsField.Value = fak.ProdavetsField.Value;
            SchetFactura.ProdavetsINNField.Value = fak.ProdavetsINNField.Value;
            SchetFactura.ProdavetsAddressField.Value = fak.ProdavetsAddressField.Value;
            SchetFactura.ProdavetsKPPField.Value = fak.ProdavetsKPPField.Value;
            SchetFactura.ProdavetsNameField.Value = fak.ProdavetsNameField.Value;

            SchetFactura.RukovoditelTextField.Value = fak.RukovoditelTextField.Value;
            SchetFactura.BuhgalterTextField.Value = fak.BuhgalterTextField.Value;

            Rukovoditel.ValueText = SchetFactura.RukovoditelTextField.ValueString;
            Buhgalter.ValueText = SchetFactura.BuhgalterTextField.ValueString;
            ShowDetails_Required("1");

            //Покупатель
            Pokupatel_InfoClear();
            SchetFactura.PokupatelField.Value = fak.PokupatelField.Value;
            SchetFactura.PokupatelINNField.Value = fak.PokupatelINNField.Value;
            SchetFactura.PokupatelAddressField.Value = fak.PokupatelAddressField.Value;
            SchetFactura.PokupatelKPPField.Value = fak.PokupatelKPPField.Value;
            SchetFactura.PokupatelNameField.Value = fak.PokupatelNameField.Value;
            ShowDetails_Required("2");

            //Договор
            SchetFactura.DogovorField.Value = fak.DogovorField.Value;
            SchetFactura.DogovorTextField.Value = fak.DogovorTextField.Value;
            SchetFactura.PrilozhenieField.Value = fak.PrilozhenieField.Value;

            //Грузоотправитель
            SchetFactura.GOPersonField.Value = fak.GOPersonField.Value;
            SchetFactura.GOPersonDataField.Value = fak.GOPersonDataField.Value;
            ShowDetails_Required("3");

            //Грузополучатель
            SchetFactura.GPPersonField.Value = fak.GPPersonField.Value;
            SchetFactura.GPPersonDataField.Value = fak.GPPersonDataField.Value;
            ShowDetails_Required("4");

            // Счет
            SchetFactura.SchetField.Value = fak.SchetField.Value;
            V3CS_RefreshSchetData();

            // Платежки
            SchetFactura.PlatezhkiField.Value = fak.PlatezhkiField.Value;
            V3CS_RefreshPlatezhkiData();

            SchetFactura.PrimechanieField.Value = fak.PrimechanieField.Value;

            CheckFieldDogovorUE();

            SetReadOnlyBY_TTN();
        }

        /// <summary>
        ///  Заполнить счет-фактуру на основе документа основания
        /// </summary>
        private void SetSchDataBy_DocumentOsnovanie(string id)
        {
            if (id.Length == 0)
            {
                SetReadOnly();

                V3CS_RefreshContactButton();
                return;
            }

            var d = new Document(id);

            SetSchetByDocument(d);
        }

        private void SetSchetByDocument(Document doc)
        {
            if (doc.Unavailable)
            {
                DocumentOsnovanie.Value = "";
                return;
            }
            if (doc.DataUnavailable)
            {
                DocumentOsnovanie.Value = "";
                return;
            }

            switch (doc.Type)
            {
                case DocTypeEnum.ТоварноТранспортнаяНакладная:
                    SetSchDataBy_TTN(doc.Id);
                    break;
                case DocTypeEnum.АктВыполненныхРаботУслуг:
                    SetSchDataBy_ActUSL(doc.Id);
                    break;
                case DocTypeEnum.Претензия:
                    SetSchDataBy_Claim(doc.Id);
                    break;

                default:
                    DocumentOsnovanie.Value = "";
                    break;

            }

            V3CS_RefreshContactButton();
        }


        /// <summary>
        /// Установить фактуру на основании документа Товарно-транспортная накладная
        /// </summary>
        private void SetSchDataBy_TTN(string _id)
        {
            V3CS_RefreshDetailsSupplier(false);

            var nkl = new TTN(_id);

            SchetFactura.CurrencyField.Value = nkl.CurrencyField.Value;

            //Продавец
            Prodavets_InfoClear();
            SchetFactura.ProdavetsField.Value = nkl.PostavschikField.Value;
            SchetFactura.ProdavetsNameField.Value = nkl.PostavschikDataField.Value;
            Prodavets_InfoSet();
            SchetFactura.ProdavetsAddressField.Value = nkl.PostavschikAddressField.Value;

            SchetFactura.RukovoditelTextField.Value = nkl.SignSupervisorField.Value;
            SchetFactura.BuhgalterTextField.Value = nkl.SignBuhgalterField.Value;
            Rukovoditel.ValueText = SchetFactura.RukovoditelTextField.ValueString;
            Buhgalter.ValueText = SchetFactura.BuhgalterTextField.ValueString;

            ShowDetails_Required("1");

            //Покупатель
            Pokupatel_InfoClear();
            SchetFactura.PokupatelField.Value = nkl.PlatelschikField.Value;
            SchetFactura.PokupatelNameField.Value = nkl.PlatelschikDataField.Value;
            SchetFactura.PokupatelAddressField.Value = nkl.PlatelschikAddressField.Value;
            Pokupatel_InfoSet();
            ShowDetails_Required("2");

            //Договор

            SchetFactura._Dogovor = nkl._Dogovor;
            SchetFactura.DogovorTextField.Value = nkl.DogovorTextField.Value;
            SchetFactura._Prilozhenie = nkl._Prilozhenie;

            SchetFactura._BillOfLading = nkl._BillOfLading;
            SchetFactura._Schets = nkl._SchetPred;
            SchetFactura._Platezhki = nkl._Platezhki;

            //Грузоотправитель
            GOPerson_InfoClear();
            SchetFactura.GOPersonField.Value = nkl.GOPersonField.Value;
            SchetFactura.GOPersonDataField.Value = nkl.GOPersonDataField.Value;
            ShowDetails_Required("3");

            //Грузополучатель
            GPPerson_InfoClear();
            SchetFactura.GPPersonField.Value = nkl.GPPersonField.Value;
            SchetFactura.GPPersonDataField.Value = nkl.GPPersonDataField.Value;
            ShowDetails_Required("4");

            // Счет
            if (nkl._SchetPred.Length > 0)
            {
                SchetFactura._Schets = nkl._SchetPred;
                V3CS_RefreshSchetData();
            }
            // Платежки
            if (nkl._Platezhki.Length > 0)
            {
                SchetFactura._Platezhki = nkl._Platezhki;
                V3CS_RefreshPlatezhkiData();
            }

            SchetFactura.PrimechanieField.Value = nkl.PrimechanieField.Value;

            CheckFieldDogovorUE();

            // Проставление корректируемого счета на основе ТТН (вытекающий счет из корректируемой ТТН - если она указана для текущего документа)
            if (nkl._CorrectingDoc.Length > 0)
            {
                TTN correctingNakl = new TTN(nkl._CorrectingDoc);

                var col = correctingNakl.GetSequelDocs(SchetFactura.OsnovanieField.DocFieldId);
                if (col.Count == 1)
                {
                    SchetFactura._CorrectingDoc = col[0].Id;
                    ShowHideCorrectingDoc(true);

                    SchetFactura fak = SchetFactura.CorrectingDoc;
                    SchetFactura.ProdavetsKPPField.Value = fak.ProdavetsKPPField.Value;
                    SchetFactura.PokupatelKPPField.Value = fak.PokupatelKPPField.Value;
                    SchetFactura.BillOfLadingField.Value = fak.BillOfLadingField.Value;

                    // Платежки должны совпадать у корректирующих документов
                    if (fak._Platezhki.Length > 0)
                    {
                        SchetFactura._Platezhki = fak._Platezhki;
                        V3CS_RefreshPlatezhkiData();
                    }
                }
            }

            SetReadOnlyBY_TTN();
        }

        /// <summary>
        /// Установить фактуру на основании Акт выполненных работ, услуг
        /// </summary>
        private void SetSchDataBy_ActUSL(string id)
        {
            V3CS_RefreshDetailsSupplier(true);

            AktUsl akt = new AktUsl(id);

            SchetFactura.CurrencyField.Value = akt.CurrencyField.Value;

            //Продавец
            Prodavets_InfoClear();
            SchetFactura.ProdavetsField.Value = akt.IspolnitelField.Value;
            Prodavets_InfoSet();
            SchetFactura.RukovoditelTextField.Value = akt.IspolnitelSuperField.Value;
            SchetFactura.BuhgalterTextField.Value = akt.BuhgalterTextField.Value;

            Rukovoditel.ValueText = SchetFactura.RukovoditelTextField.ValueString;
            Buhgalter.ValueText = SchetFactura.BuhgalterTextField.ValueString;

            ShowDetails_Required("1");

            //Покупатель
            Pokupatel_InfoClear();
            SchetFactura.PokupatelField.Value = akt.ZakazchikField.Value;
            Pokupatel_InfoSet();
            ShowDetails_Required("2");

            //Договор
            Dogovor_InfoClear();
            SchetFactura._Dogovor = akt._Dogovor;
            Dogovor_InfoSet();
            SchetFactura._Prilozhenie = akt._Prilozhenie;

            //Грузоотправитель
            GOPerson_InfoClear();
            SchetFactura.GOPersonField.Value = akt.GOPersonField.Value;
            SchetFactura.GOPersonDataField.Value = akt.GOPersonDataField.Value;
            ShowDetails_Required("3");

            //Грузополучатель
            GPPerson_InfoClear();
            SchetFactura.GPPersonField.Value = akt.GPPersonField.Value;
            SchetFactura.GPPersonDataField.Value = akt.GPPersonDataField.Value;
            ShowDetails_Required("4");

            SchetFactura.PrimechanieField.Value = akt.PrimechanieField.Value;

            CheckFieldDogovorUE();

            // Проставление корректируемого счета на основе ТТН (вытекающий счет из корректируемой ТТН - если она указана для текущего документа)
            if (akt._CorrectingDoc.Length > 0)
            {
                AktUsl correctingAkt = new AktUsl(akt._CorrectingDoc);
                var col = correctingAkt.GetSequelDocs(SchetFactura.OsnovanieField.DocFieldId);
                if (col.Count == 1)
                {
                    SchetFactura._CorrectingDoc = col[0].Id;
                    ShowHideCorrectingDoc(true);

                    SchetFactura fak = SchetFactura.CorrectingDoc;
                    SchetFactura.ProdavetsKPPField.Value = fak.ProdavetsKPPField.Value;

                    SchetFactura.PokupatelKPPField.Value = fak.PokupatelKPPField.Value;
                    SchetFactura.BillOfLadingField.Value = fak.BillOfLadingField.Value;

                    // Платежки должны совпадать у корректирующих документов

                    var plat = fak.Platezhki;
                    if (plat.Length > 0)
                    {
                        SchetFactura._Platezhki = fak._Platezhki;
                        V3CS_RefreshPlatezhkiData();
                    }

                    //Реальный поставщик
                    Supplier_InfoClear();
                    SchetFactura.SupplierField.Value = fak.SupplierField.Value;
                    SchetFactura.SupplierINNField.Value = fak.SupplierINNField.Value;
                    SchetFactura.SupplierAddressField.Value = fak.SupplierAddressField.Value;
                    SchetFactura.SupplierKPPField.Value = fak.SupplierKPPField.Value;
                    SchetFactura.SupplierNameField.Value = fak.SupplierNameField.Value;
                    ShowDetails_Required("5");
                }
            }

            SetReadOnlyBY_AktUsl();
        }

        /// <summary>
        /// Установить фактуру на основании притензии
        /// </summary>
        private void SetSchDataBy_Claim(string id)
        {
            V3CS_RefreshDetailsSupplier(false);

            var clm = new Claim(id);

            // валюта
            SchetFactura.CurrencyField.Value = clm.CurrencyField.Value;

            //Продавец
            Prodavets_InfoClear();
            SchetFactura.ProdavetsField.Value = clm.ZayavitelField.Value;
            Prodavets_InfoSet();
            ShowDetails_Required("1");

            //Покупатель
            Pokupatel_InfoClear();
            SchetFactura.PokupatelField.Value = clm.NarushitelField.Value;
            Pokupatel_InfoSet();
            ShowDetails_Required("2");

            //Договор
            Dogovor_InfoClear();
            SchetFactura._Dogovor = clm._Dogovor;
            Dogovor_InfoSet();
            SchetFactura._Prilozhenie = clm._Prilozhenie;

            //Грузоотправитель
            GOPerson_InfoClear();

            //Грузополучатель
            GPPerson_InfoClear();

            CheckFieldDogovorUE();
            SetReadOnlyBY_Claim();
        }

        /// <summary>
        /// Установить режим только чтения для ТТН
        /// </summary>
        private void SetReadOnlyBY_TTN()
        {
            if (SchetFactura._Osnovanie.Length == 0) return;

            Currency.IsReadOnly = !SchetFactura.CurrencyField.IsValueEmpty || !DocEditable;
            Prodavets.IsReadOnly = !SchetFactura.ProdavetsField.IsValueEmpty || !DocEditable;
            ProdavetsName.IsReadOnly = !SchetFactura.ProdavetsNameField.IsValueEmpty || !DocEditable;
            ProdavetsAddress.IsReadOnly = !SchetFactura.ProdavetsAddressField.IsValueEmpty || !DocEditable;

            Pokupatel.IsReadOnly = !SchetFactura.PokupatelField.IsValueEmpty || !DocEditable;
            PokupatelName.IsReadOnly = !SchetFactura.PokupatelNameField.IsValueEmpty || !DocEditable;
            PokupatelAddress.IsReadOnly = !SchetFactura.PokupatelAddressField.IsValueEmpty || !DocEditable;

            Dogovor.IsReadOnly = SchetFactura._Dogovor.Length > 0 || !DocEditable;
            //DogovorText.IsReadOnly = !SchetFactura.DogovorTextField.IsValueEmpty || !DocEditable;
           // Prilozhenie.IsReadOnly = SchetFactura._Prilozhenie.Length > 0 || !DocEditable;
            BillOfLading.IsReadOnly = SchetFactura._BillOfLading.Length > 0 || !DocEditable;
            Schet.IsReadOnly = SchetFactura._Schets.Length > 0 || !DocEditable;
            Platezhki.IsReadOnly = SchetFactura._Platezhki.Length > 0 || !DocEditable;

            GOPerson.IsReadOnly = !DocEditable;
           
            GPPerson.IsReadOnly = !DocEditable;

            // При наличии корректируемого счета делаем нередактируемыми поля
            if (SchetFactura._CorrectingDoc.Length > 0)
            {
                Dogovor.IsReadOnly = Prilozhenie.IsReadOnly = BillOfLading.IsReadOnly =
                Platezhki.IsReadOnly =
                Schet.IsReadOnly =
                SupplierKPP.IsReadOnly =
                ProdavetsKPP.IsReadOnly =
                    // RukovoditelText.IsReadOnly =
                    // BuhgalterText.IsReadOnly =
                PokupatelKPP.IsReadOnly = true;
            }
        }

        /// <summary>
        ///  Установить режим только чтения для документа Акт выполненных работ, услуг
        /// </summary>
        private void SetReadOnlyBY_AktUsl()
        {
            if (SchetFactura._Osnovanie.Length == 0) return;

            Currency.IsReadOnly = !SchetFactura.CurrencyField.IsValueEmpty || !DocEditable;
            Supplier.IsReadOnly = !SchetFactura.SupplierField.IsValueEmpty || !DocEditable;
            Prodavets.IsReadOnly = !SchetFactura.ProdavetsField.IsValueEmpty || !DocEditable;
            Pokupatel.IsReadOnly = !SchetFactura.PokupatelField.IsValueEmpty || !DocEditable;
            Dogovor.IsReadOnly = SchetFactura._Dogovor.Length > 0 || !DocEditable;
            //Prilozhenie.IsReadOnly = SchetFactura._Prilozhenie.Length > 0 || !DocEditable;
            GOPerson.IsReadOnly = !SchetFactura.GOPersonField.IsValueEmpty || !DocEditable;
           // GOPersonData.IsReadOnly = !SchetFactura.GOPersonDataField.IsValueEmpty || !DocEditable;
            GPPerson.IsReadOnly = !SchetFactura.GPPersonField.IsValueEmpty || !DocEditable;
           // GPPersonData.IsReadOnly = !SchetFactura.GPPersonDataField.IsValueEmpty || !DocEditable;

            // При наличии корректируемого счета делаем нередактируемыми поля
            if (SchetFactura._CorrectingDoc.Length > 0)
            {
                Dogovor.IsReadOnly = Prilozhenie.IsReadOnly = BillOfLading.IsReadOnly =
                    Platezhki.IsReadOnly =
                    Schet.IsReadOnly =
                    SupplierKPP.IsReadOnly =
                    ProdavetsKPP.IsReadOnly =
                    // RukovoditelText.IsReadOnly =
                    // BuhgalterText.IsReadOnly =
                    PokupatelKPP.IsReadOnly = true;
            }
        }

        /// <summary>
        ///  Установить режим только чтения для документа Претензия
        /// </summary>
        private void SetReadOnlyBY_Claim()
        {
            if (SchetFactura._Osnovanie.Length == 0) return;

            Currency.IsReadOnly = !SchetFactura.CurrencyField.IsValueEmpty || !DocEditable;
            Prodavets.IsReadOnly = !SchetFactura.ProdavetsField.IsValueEmpty || !DocEditable;
            Pokupatel.IsReadOnly = !SchetFactura.PokupatelField.IsValueEmpty || !DocEditable;
            Dogovor.IsReadOnly = SchetFactura._Dogovor.Length > 0 || !DocEditable;
            //Prilozhenie.IsReadOnly = SchetFactura._Prilozhenie.Length > 0 || !DocEditable;
        }

        #endregion

        private void SetDocSigners()
        {
            if (SchetFactura.ProdavetsField.IsValueEmpty || !SchetFactura.RukovoditelTextField.IsValueEmpty) return;
            DataTable dt = SchetFactura.GetDocSigners();
            if (dt.Rows.Count == 0) return;

            SchetFactura.RukovoditelTextField.Value = dt.Rows[0]["Text100_2"].ToString();
            SchetFactura.BuhgalterTextField.Value = dt.Rows[0]["Text100_3"].ToString();

            Rukovoditel.ValueText = SchetFactura.RukovoditelTextField.ValueString;
            Buhgalter.ValueText = SchetFactura.BuhgalterTextField.ValueString;
        }

        /// <summary>
        ///  Вывести обычный браузерный алерт
        /// </summary>
        public void V3CS_Alert(string message)
        {
            JS.Write("alert('{0}');", message);
        }

        /// <summary>
        /// Вывод общей суммы по документу. Выполняется сравнение с Money1 и в случае несоответствия вывод Money1 [&lt;сумма позиций&gt;]
        /// </summary>
        /// <param name="dVsego">сумма позиций, которую сравниваем с Money1</param>
        /// <returns>строковое представление ВСЕГО</returns>
        protected string RenderVsegoMoney1(decimal dVsego)
        {
            int scale = Doc.CurrencyScale;
            var money1 = Doc.DocumentData.Money1 == null ? "" : Doc.DocumentData.Money1.ToString();
            string money = FormatNumber(money1, scale, scale, " ");
            string vsego = FormatNumber(Kesco.Lib.ConvertExtention.Convert.Decimal2Str(dVsego, scale), scale, scale, " ");

            if (!money.Equals(vsego)) money += " [" + vsego + "]";
            return money;
        }

        private string FormatNumber(string n, int minScale, int maxScale, string groupSeparator)
        {
            if (n.Length == 0) return "";
            decimal d = Kesco.Lib.ConvertExtention.Convert.Str2Decimal(n);
            NumberFormatInfo nfi = (NumberFormatInfo)NumberFormatInfo.CurrentInfo.Clone();
            nfi.CurrencySymbol = "";
            nfi.CurrencyDecimalDigits = maxScale;
            nfi.CurrencyGroupSeparator = groupSeparator;
            string s = d.ToString("C", nfi);

            s = Regex.Replace(s, "[0]{0," + (maxScale - minScale) + "}$", "");
            s = Regex.Replace(s, ",$", "");

            return s;
        }

        /// <summary>
        ///  Удалить документ основание
        /// </summary>
        private void RemoveBaseDoc(int docId, int fieldId)
        {
            Doc.RemoveBaseDoc(docId, fieldId);
        }

        private void RenderTableUsl(TextWriter w, DataTable tbl, Currency currency)
        {
            int minScale = currency != null ? currency.UnitScale : 2, maxScale = 4;

            foreach (DataRow row in tbl.Rows)
            {
                string _ResourceRus = Convert.IsDBNull(row["РесурсРус"]) ? "" : row["РесурсРус"].ToString();
                string _Kolichestvo = Convert.IsDBNull(row["Количество"]) ? "" : row["Количество"].ToString();
                string _Unit = Convert.IsDBNull(row["ЕдиницаРус"]) ? "" : row["ЕдиницаРус"].ToString();
                string _UnitId = Convert.IsDBNull(row["КодЕдиницыИзмерения"]) ? "" : row["КодЕдиницыИзмерения"].ToString();
                string _CostOutNDS = Convert.IsDBNull(row["ЦенаБезНдс"]) ? "" : row["ЦенаБезНдс"].ToString();
                string _StavkaNDS = Convert.IsDBNull(row["СтавкаНдс"]) ? "" : row["СтавкаНдс"].ToString();
                string _SummaOutNDS = Convert.IsDBNull(row["СуммаБезНдс"]) ? "" : row["СуммаБезНдс"].ToString();
                string _SummaNDS = Convert.IsDBNull(row["СуммаНдс"]) ? "" : row["СуммаНдс"].ToString();
                string _Vsego = Convert.IsDBNull(row["Всего"]) ? "" : row["Всего"].ToString();
                string codRes = Convert.IsDBNull(row["КодРесурса"]) ? "" : row["КодРесурса"].ToString();

                w.Write("<tr>");

                NumberFormatInfo nfiDouble = (NumberFormatInfo)NumberFormatInfo.CurrentInfo.Clone();
                nfiDouble.CurrencySymbol = "";
                nfiDouble.CurrencyDecimalDigits = 0;

                //Ресурс
                w.Write("<td>");
                if (codRes.Length > 0)
                {
                    nfiDouble.CurrencyDecimalDigits = new Resource(codRes).Accuracy;

                    RenderLinkResource(w, codRes);
                    w.Write(_ResourceRus);
                    RenderLinkEnd(w);
                }
                w.Write("</td>");

                //Количество и Ед.Измерения
                w.Write("<td align='right' nowrap>{0}</td><td align='left'>{1}</td>",
                    _Kolichestvo.Length == 0 ? "" : Kesco.Lib.ConvertExtention.Convert.Str2Decimal(_Kolichestvo).ToString("C", nfiDouble),
                    _Unit.Length == 0 ? "&nbsp;" : _Unit
                );

                //Цена без НДС
                w.Write("<td align='right' nowrap>");
                RenderNumber(w, Kesco.Lib.ConvertExtention.Convert.Str2Decimal(_CostOutNDS).ToString("N2"), minScale, maxScale, " ");
                w.Write("</td>");

                //%
                w.Write("<td align='center' nowrap>");
                w.Write(_StavkaNDS);
                w.Write("</td>");

                //Сумма без НДС
                w.Write("<td align='right' nowrap>");
                RenderNumber(w, _SummaOutNDS, minScale, maxScale, " ");
                w.Write("</td>");

                //НДС
                w.Write("<td align='right' nowrap>");
                RenderNumber(w, _SummaNDS, minScale, maxScale, " ");
                w.Write("</td>");

                //Всего
                w.Write("<td align='right' nowrap>");
                RenderNumber(w, _Vsego, minScale, maxScale, " ");
                w.Write("</td>");

                w.Write("</tr>");
            }
        }

        /// <summary>
        /// Проверка на то, что дата вытек.документа >= дате основания
        /// </summary>
        protected void ValidateBaseDocDate(Document baseDoc, Ntf ntf)
        {
            if (baseDoc != null && baseDoc.Available)
            {
                if (SchetFactura.Date < baseDoc.Date)
                {
                    var ntfMess = Resx.GetString("NTF_DateBaseDoc");
                    if (!ntf.Contains(ntfMess))
                    {
                        ntf.Clear();
                        ntf.Add(ntfMess, NtfStatus.Error);
                    }
                }
            }
        }

        /// <summary>
        ///  Событие изменения приложения
        /// </summary>
        protected void Prilozhenie_OnChanged(object sender, ProperyChangedEventArgs e)
        {
            if (e.NewValue != e.OldValue)
            {
                if (string.IsNullOrEmpty(e.NewValue))
                    Doc.RemoveAllBaseDocs(SchetFactura.PrilozhenieField.DocFieldId);
                else
                    Doc.AddBaseDoc(e.NewValue, SchetFactura.PrilozhenieField.DocFieldId);
            }
        }

        /// <summary>
        ///  Событие изменения коносмент
        /// </summary>
        protected void BillOfLading_OnChanged(object sender, ProperyChangedEventArgs e)
        {
            if (e.NewValue != e.OldValue)
            {
                if (string.IsNullOrEmpty(e.NewValue))
                    Doc.RemoveAllBaseDocs(SchetFactura.BillOfLadingField.DocFieldId);
                else
                    Doc.AddBaseDoc(e.NewValue, SchetFactura.BillOfLadingField.DocFieldId);
            }
        }
    }
}