using System;
using System.Collections.Specialized;
using System.Data;
using System.Web.UI;
using Kesco.Lib.BaseExtention;
using Kesco.Lib.BaseExtention.Enums.Controls;
using Kesco.Lib.BaseExtention.Enums.Docs;
using Kesco.Lib.DALC;
using Kesco.Lib.Entities.Documents;
using Kesco.Lib.Web.Controls.V4;
using Kesco.Lib.Web.DBSelect.V4;
using Kesco.Lib.Web.Settings;
using Page = Kesco.Lib.Web.Controls.V4.Common.Page;

namespace Trade
{
    /// <summary>
    ///  Веб диалог создания вытекающего документа
    /// </summary>
    public partial class linkedDoc : Page
    {
        protected override string HelpUrl { get; set; }

        protected DBSDocument LinkedDoc;

        private string _newValue;

        private string _type;

        private string _Id;

        private string _field;

        /// <summary>
        ///  Загрузка страницы
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            _type = Request["type"];
            _Id = Request["Id"];
            _field = Request["field"];

            string basedate = Request["basedate"];

            string linkedDocs = Request["linkedDocs"];

            LinkedDoc = new DBSDocument();
            LinkedDoc.V4Page = this;
            LinkedDoc.HtmlID = "linkedDoc";
            LinkedDoc.Width = new System.Web.UI.WebControls.Unit("350px");

            if (!string.IsNullOrEmpty(_type))		// тип документа
            {
                LinkedDoc.Filter.Type.Add(_type, DocTypeQueryType.Equals);
            }
            if (!string.IsNullOrEmpty(basedate))	// дата вытекающего больше даты основания
            {
                //var date = DateTime.ParseExact( basedate, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture );
                LinkedDoc.Filter.Date.DateSearchType = DateSearchType.MoreThan;
                LinkedDoc.Filter.Date.Add(basedate);
            }
            if (!string.IsNullOrEmpty(linkedDocs))	// исключение уже привязанных документов
            {
                var col = Kesco.Lib.ConvertExtention.Convert.Str2Collection(linkedDocs);
                foreach (string id in col)
                    LinkedDoc.Filter.IDs.Add(id);

                if (col.Count > 0)
                    LinkedDoc.Filter.IDs.Inverse = true;		// указанные Ids не подходят
            }

            LinkedDoc.OnRenderNtf += LinkedDocOnOnRenderNtf;
            LinkedDoc.ValueChanged += LinkedDocOnValueChanged;
            V4Controls.Add(LinkedDoc);
        }

        /// <summary>
        ///  Обработка клиентских команд
        /// </summary>
        protected override void ProcessCommand(string cmd, NameValueCollection param)
        {
            switch (cmd)
            {
                case "OK":
                    string url = "";
                    bool bAddActive = param["arg0"] == "true";
                    bool bChooseActive = param["arg1"] == "true";

                    if ((bAddActive || bChooseActive) && !string.IsNullOrEmpty(_type))
                    {
                        DocType dtp = new DocType(_type);

                        // Если связываем существующий документ
                        if (!bAddActive && bChooseActive && LinkedDoc.Value != "")
                            url = dtp.URL + (dtp.URL.IndexOf("?", StringComparison.Ordinal) > 0 ? "&" : "?") +
                                "DocId=" + _Id + "&Id=" + LinkedDoc.Value +
                                "&fieldId=" + _field;
                        // создаем новый документ
                        else if (bAddActive && !bChooseActive)
                            url = dtp.URL + (dtp.URL.IndexOf("?", StringComparison.Ordinal) > 0 ? "&" : "?") + "DocId=" + _Id +
                                "&fieldId=" + _field;
                    }

                    if (url != "")
                    {
                        JS.Write("win=window.open('{0}','_blank','status=no,toolbar=no,menubar=no,location=no,resizable=yes,scrollbars=yes');", url);

                        Close();
                        JS.Write("try{setTimeout(win.focus(),0);} catch(e){}");
                    }
                    break;
                default:
                    base.ProcessCommand(cmd, param);
                    break;
            }
        }

        /// <summary>
        ///  Отрисовка диалога
        /// </summary>
        protected override void Render(HtmlTextWriter writer)
        {
            base.Render(writer);
            RenderBody(writer);
        }

        /// <summary>
        ///  Рендер body
        /// </summary>
        public void RenderBody(System.IO.TextWriter w)
        {
            w.Write("<input type='radio' name='action-type' onclick='enableSelect(false)' id='add' {0}><label for='add'>{0}</label><br/>", Resx.GetString("create"));
            w.Write("<input type='radio' name='action-type' onclick='enableSelect(true)' id='choose' ><label for='choose'>{0}</label>", Resx.GetString("add"));

            HtmlTextWriter htw = new HtmlTextWriter(w);
            w.Write("<div style=\"padding:5px;\">");
            LinkedDoc.RenderControl(htw);
            w.Write("</div>");
            w.Write("<div style=\"padding:5px;\">");
            w.Write("<input id='OK' type='button' style='BACKGROUND: url(/Styles/ok.gif) buttonface no-repeat left center; HEIGHT: 25px' onclick=\"if(document.activeElement==this) cmd('cmd', 'OK', 'arg0', document.all('add').checked, 'arg1', document.all('choose').checked);\" value='{0}' value='1'/>", Resx.GetString("BtnConfirm"));
            w.Write("<input id='btCancel' type='button' style='BACKGROUND: url(/Styles/cancel.gif) buttonface no-repeat left center; HEIGHT: 25px' onclick=\"if(document.activeElement==this) window.close();\" value='  {0}'/>", Resx.GetString("ppBtnCancel"));
            w.Write("</div>");

            // скрипты блокирования/разблокирования контрола выбора документа
            w.Write(@"
<script language='javascript'>
<!--
	function enableSelect( enable )
	{
		if( enable )
		{
			if( document.all('linkedDoc_0') != null ) document.all('linkedDoc_0').disabled='';
			if( document.all('linkedDoc_1') != null ) document.all('linkedDoc_1').disabled='';
			//if( document.all('linkedDocBtnDetails') != null ) document.all('linkedDocBtnDetails').disabled='';
		}
		else
		{
         // cmd('ctrl', 'linkedDoc', 'tn', o.value, 'cmd', 'popup');
		 //	cmd( 'V3EV_Control', 'linkedDoc', 'TextChanged' );
			if( document.all('linkedDoc_0') != null ) document.all('linkedDoc_0').disabled='disabled';
			if( document.all('linkedDoc_1') != null ) document.all('linkedDoc_1').disabled='disabled';
		}
	}

	document.title='" + Resx.GetString("title") + @"';
	document.all('add').checked=true;
	enableSelect(false);
-->
</script>");
        }

        /// <summary>
        /// Проверка соответствия выбранного документа ограничениям по типу связи
        /// </summary>
        private void LinkedDocOnOnRenderNtf(object sender, Ntf ntf)
        {
            if (!string.IsNullOrEmpty(_newValue))
            {
                ntf.Clear();
                // Проверка выбранного документа на подписанность
                Document akt = new Document(_newValue);
                if (akt.Signed)
                    ntf.Add(Resx.GetString("signedAkt"));
                string linkType = "";
                if (V4Request["linkType"] != null) linkType = V4Request["linkType"];

                // В данном случае требуется выполнить проверку, исключающую выбранный документ в случае,
                // если он уже является вытекающим из другого документа по этому же полю
                if (linkType == "11" || linkType == "12")
                {
                    string field;
                    if (V4Request["field"] != null) field = V4Request["field"];
                    else return;

                    string sqlQuery = string.Format(@"
                    SELECT COUNT(*) FROM vwСвязиДокументов
                    WHERE КодПоляДокумента = {0} AND КодДокументаВытекающего = {1}", field, _newValue);

                    var result = DBManager.ExecuteScalar(sqlQuery, CommandType.Text, Config.DS_document);
                    int count;

                    if (result == null)
                        count = 0;
                    else
                        int.TryParse(result.ToString(), out count);

                    if (count > 0)
                    {
                        ntf.Add(Resx.GetString("alertAlreadyLinked"));
                        JS.Write("if( document.all('OK') != null ) document.all('OK').disabled='disabled';");
                        return;
                    }
                }
            }

            JS.Write("if( document.all('OK') != null ) document.all('OK').disabled='';");
        }


        /// <summary>
        ///  Событие изменения 
        /// </summary>
        private void LinkedDocOnValueChanged(object sender, ValueChangedEventArgs e)
        {
            _newValue = e.NewValue;
        }
    }
}