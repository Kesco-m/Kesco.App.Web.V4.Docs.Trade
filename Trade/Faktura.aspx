<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Faktura.aspx.cs" Inherits="Trade.Factura" %>
<%@ Register TagPrefix="cc" Namespace="Kesco.Lib.Web.Controls.V4" Assembly="Controls.V4" %>
<%@ Register TagPrefix="cs" Namespace="Kesco.Lib.Web.DBSelect.V4" Assembly="DBSelect.V4" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html>
<head runat="server">
    <title></title>
    <link href="Style/Style.css" rel="stylesheet" type="text/css" />
    <script src="Script/FacturaJS.js" type="text/javascript"></script>
</head>
<body>
<%=RenderDocumentHeader()%>
<br/>
   <div class="v4formContainer">
        <div class="ctlMargMain">
           <%RenderDocNumDateNameRows(Response.Output);%>
        </div>
        
        <div class="ctlMargMain">
           <%if(DocEditable&&!IsPrintVersion&&!IsInDocView){%><span title="<%=SchetFactura.CorrectingFlagField.Description%>"><%=SchetFactura.CorrectingFlagField.Name%></span>:
	       <cc:CheckBox ID="flagCorrecting" runat="server" OnChanged="flagCorrecting_OnChanged" NextControl="CorrectingDoc"></cc:CheckBox><%}%>
            <span id="tdCorrectingDoc" width="100%" style="display: none;">
                            <cs:DBSDocument id="CorrectingDoc" runat="server" OnChanged="CorrectingDoc_OnChanged" OnBeforeSearch="CorrectingDoc_OnBeforeSearch" NextControl="DateProvodki"></cs:DBSDocument></span>
            <div style="color: red;">
                <span id="CorrectingSequel" colspan="2"><% RenderCorrectingSequel(Response.Output); %></span>
            </div> 
        </div>
        
        <hr/>
 
        <!--Дата проводки-->       
        <div class="ctlMargMain">
           <div class="inl wd" title="<%= SchetFactura.DateProvodkiField.Description%>"><%=SchetFactura.DateProvodkiField.Name%>:</div>
           <div class="inl al"><cc:DatePicker id="DateProvodki" runat="server" Width="100%" NextControl="DocumentOsnovanie"></cc:DatePicker></div>
        </div>
        
        <hr/>
        <!--Документ основание-->
        <div class="ctlMargMain">
           <div class="inl wd" title="<%= SchetFactura.OsnovanieField.Description%>"><%=SchetFactura.OsnovanieField.Name%>:</div>
	       <div class="inl al"><cs:DBSDocument id="DocumentOsnovanie" Width="300px" OnChanged="DocumentOsnovanie_Changed" NextControl="Currency" runat="server"></cs:DBSDocument></div>
        </div>
        
        <hr/>
        <!--Валюта оплаты-->
        <div class="ctlMargMain">
        		<div class="inl wd" title="<%=SchetFactura.CurrencyField.Description%>"> <%=SchetFactura.CurrencyField.Name%>:</div>
				<div class="inl al"><cs:DBSResource id="Currency" IsAlwaysAdvancedSearch="False" MaxItemsInPopup="20" Width="100px" NextControl="Supplier" runat="server"></cs:DBSResource></div>
        </div>
        
        <hr/>
        <!--Реальный поставщик-->
        <div id="tr_supplier" class="ctlMargMain" style="display: none">
            <div class="inl wd" title="<%=SchetFactura.SupplierField.Description%>"><%=SchetFactura.SupplierField.Name%>:
            <img id="SupplierUpDown" src="/styles/ScrollDownEnabled.gif" 
             title="показать/скрыть информацию по реальному поставщику" border='0' 
             onclick="cmd('cmd','DownUp','arg0','5');" onkeydown="if(event.keyCode === 13){event.returnValue=false;cmd('cmd','DownUp','arg0','5');}" style="cursor: pointer">
            </div>
			<div class="inl al"><cs:DBSPerson id="Supplier" IsCaller="True" Width="300px" NextControl="SupplierName" runat="server"></cs:DBSPerson>
            	
             <div id="SupplierProp" style="display: none;">		
                <!--Реальный поставщик-->
			    <div id="tr5_0" title="<%=SchetFactura.SupplierNameField.Description%>"><%=SchetFactura.SupplierNameField.Name%>:
			    <cc:TextBox ID="SupplierName" IsReadOnly="True" NextControl="SupplierINN" runat="server"></cc:TextBox></div>
                
                <!--ИНН поставщика-->
			    <div id="tr5_1" title="<%=SchetFactura.SupplierINNField.Description%>"><%=SchetFactura.SupplierINNField.Name%>:
			    <cc:TextBox ID="SupplierINN" IsReadOnly="True" NextControl="SupplierKPP" runat="server"></cc:TextBox></div>
                
                <!--КПП поставщика-->
			    <div id="tr5_2" style="padding-left: 20px; float: left; font-weight:bold" title="<%=SchetFactura.SupplierKPPField.Description%>"> <%=SchetFactura.SupplierKPPField.Name%>:
			    <span><cc:TextBox ID="SupplierKPP" NextControl="SupplierAddress" runat="server"></cc:TextBox></span></div>
                
                <!--Адрес поставщика-->
			    <div id="tr5_3" title="<%=SchetFactura.SupplierAddressField.Description%>"><%=SchetFactura.SupplierAddressField.Name%> : <span id="Contact5"></span>
			    <span><cc:TextBox ID="SupplierAddress" IsReadOnly="True" NextControl="Prodavets" runat="server"></cc:TextBox></span></div>
            </div>

            </div>
        </div>
        
        <!--Продавец-->
        <div class="ctlMargMain">
            		
				<div class="inl wd"  title="<%=SchetFactura.ProdavetsField.Description%>"><%=SchetFactura.ProdavetsField.Name%>:
                <img id="ProdavetsUpDown" src="/styles/ScrollDownEnabled.gif" border='0'
                 title="показать/скрыть информацию по продавцу"
                 onclick="cmd('cmd','DownUp','arg0','1');" onkeydown="if(event.keyCode === 13){event.returnValue=false;cmd('cmd','DownUp','arg0','1');}" style="cursor: pointer"></div>
                <div class="inl al"><cs:DBSPerson IsCaller="True" id="Prodavets" Width="300px" NextControl="ProdavetsName" runat="server"></cs:DBSPerson>
                
                <div id="ProdavetsProp" style="display: none;">
                   <!--Название продавца-->
				   <div id="tr1_0" title="<%=SchetFactura.ProdavetsNameField.Description%>"><%=SchetFactura.ProdavetsNameField.Name%>:
				   <cc:TextBox ID="ProdavetsName" IsReadOnly="True" NextControl="ProdavetsINN" runat="server"></cc:TextBox>
                   </div>
                     
                   <div id="tr1_1">
                       <!--ИНН продавца-->   
				       <div class="inl" style="padding-right: 150px;" title="<%=SchetFactura.ProdavetsINNField.Description%>"><%=SchetFactura.ProdavetsINNField.Name%>:
				       <cc:TextBox ID="ProdavetsINN" IsReadOnly="True" NextControl="ProdavetsKPP" runat="server"></cc:TextBox>
                       </div>
                   
                       <!--КПП продавца--> 
				       <div class="inl" title="<%=SchetFactura.ProdavetsKPPField.Description%>"><%=SchetFactura.ProdavetsKPPField.Name%>:
				       <cc:TextBox ID="ProdavetsKPP" NextControl="ProdavetsAddress" runat="server"></cc:TextBox>
                       </div>
                   </div>
                   
                   <!--Адрес продавца-->
				   <div id="tr1_2" title="<%=SchetFactura.ProdavetsAddressField.Description%>"><%=SchetFactura.ProdavetsAddressField.Name%> :
                    <span id="Contact1"></span>
				    <span><cc:TextBox ID="ProdavetsAddress" NextControl="Rukovoditel" IsReadOnly="True" runat="server"></cc:TextBox></span>
                   </div>
                   
                   <div id="tr1_3"></div>
                   
                   <hr/>
                     
                   <!--ФИО руководителя-->
                   <div id="tr1_4">
				     <div class="inl wd" title="<%=SchetFactura.RukovoditelTextField.Description%>"><%=SchetFactura.RukovoditelTextField.Name%>:</div>
                     <div class="inl al"><cs:DBSPerson id="Rukovoditel" IsCaller="True" CallerType="Person" NextControl="Buhgalter" runat="server" Width="150px"></cs:DBSPerson></div>
                   </div>
                   
                   <!--ФИО бухгалтера-->
                   <div id="tr1_5">
			          <div class="inl wd" title="<%=SchetFactura.BuhgalterTextField.Description%>"><%=SchetFactura.BuhgalterTextField.Name%>:</div>
                      <div class="inl al"><cs:DBSPerson id="Buhgalter" IsCaller="True" CallerType="Person" NextControl="Pokupatel" runat="server" Width="150px"></cs:DBSPerson></div>
                   </div>

                   <div id="tr1_6"></div>

                    <hr/>
                   </div>
                </div>
        </div>
        
        <!--Покупатель-->
        <div class="ctlMargMain">
            <div class="inl wd" title="<%=SchetFactura.PokupatelField.Description%>"><%=SchetFactura.PokupatelField.Name%>: 
            <img id="PokupatelUpDown" src="/styles/ScrollDownEnabled.gif" border='0'
             title="показать/скрыть информацию по покупателю"
             onclick="cmd('cmd','DownUp','arg0','2');" onkeydown="if(event.keyCode === 13){event.returnValue=false;cmd('cmd','DownUp','arg0','2');}" style="cursor: pointer"></div>
		    <div class="inl al"><cs:DBSPerson id="Pokupatel" IsCaller="True" Width="300px" NextControl="PokupatelName" runat="server"></cs:DBSPerson>
            
            <div id="PokupatelProp" style="display: none;">
                <!--Название покупателя-->
				<div id="tr2_0" title="<%=SchetFactura.PokupatelNameField.Description%>"><%=SchetFactura.PokupatelNameField.Name%>: 
                <cc:TextBox ID="PokupatelName" IsReadOnly="True" NextControl="PokupatelINN" runat="server"></cc:TextBox>
                
                <!--полное наименование-->
                <%-- <%if(DocEditable && !IsPrintVersion){%>
                    <span style="padding-left: 50px;"><cc:CheckBox id="fFullName" Text=" полное наименование" runat="server"></cc:CheckBox></span>
				<%}%>--%>
                </div>
				
                <div id="tr2_1">
                <!--ИНН покупателя--> 			
				    <div class="inl" style="padding-right: 150px;" title="<%=SchetFactura.PokupatelINNField.Description%>"><%=SchetFactura.PokupatelINNField.Name%>: 
                    <cc:TextBox ID="PokupatelINN" IsReadOnly="True" NextControl="PokupatelKPP" runat="server"></cc:TextBox>
                    </div>
                
                    <!--КПП покупателя--> 
				    <div class="inl" title="<%=SchetFactura.PokupatelKPPField.Description%>"><%=SchetFactura.PokupatelKPPField.Name%>: 
				    <cc:TextBox ID="PokupatelKPP" NextControl="PokupatelAddress" runat="server"></cc:TextBox></div>
                </div>
                
                <!--Адрес покупателя-->
				<div id="tr2_2" title="<%=SchetFactura.PokupatelAddressField.Description%>"><%=SchetFactura.PokupatelAddressField.Name%>: 
                <span id="Contact2"></span>
                <cc:TextBox ID="PokupatelAddress" IsReadOnly="True" NextControl="Dogovor" runat="server"></cc:TextBox>
				</div>
              </div>
            </div>
        </div>
        
        <hr/>
        <!--Договор-->
        <div class="ctlMargMain">
            <div class="inl wd" title="<%=SchetFactura.DogovorField.Description%>"><%=SchetFactura.DogovorField.Name%>:</div>
            <div class="inl al"><cs:DBSDocument id="Dogovor" Width="300px" NextControl="Prilozhenie" runat="server"></cs:DBSDocument>
            <div id="divKurator"><%RenderDogovorKurator(Response.Output);%></div>
            </div>
        </div>
        
        <!--Приложение-->
        <div class="ctlMargMain">
            <% if (DocEditable || SchetFactura._Prilozhenie.Length > 0){%>

				<div class="inl wd" title="<%=SchetFactura.PrilozhenieField.Description%>"><%=SchetFactura.PrilozhenieField.Name%>:</div>
                <div class="inl al"><cs:DBSDocument id="Prilozhenie" Width="300px" OnChanged="Prilozhenie_OnChanged" NextControl="BillOfLading" runat="server"></cs:DBSDocument></div>

			<% }%>
        </div>
        
        <!--Коносамент-->
        <div class="ctlMargMain">
            <% if (DocEditable || SchetFactura._BillOfLading.Length > 0){%>
				<div class="inl wd" title="<%=SchetFactura.BillOfLadingField.Description%>"><%=SchetFactura.BillOfLadingField.Name%>:</div>
                <div class="inl al"><cs:DBSDocument id="BillOfLading" Width="300px" OnChanged="BillOfLading_OnChanged" NextControl="GOPerson" runat="server"></cs:DBSDocument></div>
			<% }%>
        </div>
        
        <hr/>
        <!--Грузоотправитель-->
        <div class="ctlMargMain">
            <div class="inl wd" title="<%=SchetFactura.GOPersonField.Description%>"><%=SchetFactura.GOPersonField.Name%>: 
            <img id="GOPersonUpDown" src="/styles/ScrollDownEnabled.gif" border='0' 
             title="показать/скрыть информацию по грузоотправителю"
             onclick="cmd('cmd','DownUp','arg0','3');" onkeydown="if(event.keyCode === 13){event.returnValue=false;cmd('cmd','DownUp','arg0','3');}" style="cursor: pointer"></div>
            <div class="inl al"><cs:DBSPerson id="GOPerson" IsCaller="True" Width="300px" NextControl="GOPersonData" runat="server"></cs:DBSPerson>
            
            <!--Реквизиты грузоотправителя-->
            <div id="GOPersonProp" style="display: none;">
              <div id="tr3_0" title="<%=SchetFactura.GOPersonDataField.Description%>"><%=SchetFactura.GOPersonDataField.Name%>:
                <span id="Contact3"></span>
				<div><cc:TextBox ID="GOPersonData" IsReadOnly="True" NextControl="GPPerson" runat="server"></cc:TextBox></div>
                </div>
            </div>
        </div>
        </div>
        
        <!--Грузополучатель-->
        <div class="ctlMargMain">
            <div class="inl wd" title="<%=SchetFactura.GPPersonField.Description%>"><%=SchetFactura.GPPersonField.Name%>: 
            <img id="GPPersonUpDown" src="/styles/ScrollDownEnabled.gif" border='0'
             title="показать/скрыть информацию по грузополучателю"
             onclick="cmd('cmd','DownUp','arg0','4');" onkeydown="if(event.keyCode === 13){event.returnValue=false;cmd('cmd','DownUp','arg0','4');}" style="cursor: pointer"></div>
			<div class="inl al"><cs:DBSPerson id="GPPerson" IsCaller="True" Width="300px" NextControl="GPPersonData" runat="server"></cs:DBSPerson>

            <!--Реквизиты грузополучателя-->
            <div id="GPPersonProp" style="display: none;">
                  <div id="tr4_0" title="<%=SchetFactura.GPPersonDataField.Description%>"><%=SchetFactura.GPPersonDataField.Name%>:
                     <span id="Contact4"></span>
			   	  <div><cc:TextBox ID="GPPersonData" IsReadOnly="True" NextControl="Schet" runat="server"></cc:TextBox></div>
		         </div>
            </div>
            
            </div>
        </div>
 
        <hr/>
        <!--Счет, Инвойс проформа-->
        <div class="ctlMargMain">
             <div class="inl wd" title="<%=SchetFactura.SchetField.Description%>"><%=SchetFactura.SchetField.Name%>:</div>
			 <div class="inl al"><cs:DBSDocument id="Schet" Width="300px" NextControl="Platezhki" IsMultiSelect="True" IsRemove="True" ConfirmRemove="True" OnChanged="Schet_OnChanged" OnDeleted="Schet_OnDeleted" runat="server"></cs:DBSDocument></div>
	    </div>
        
        <!--Платежные документы-->
        <div class="ctlMargMain">
            <div class="inl wd" title="<%=SchetFactura.PlatezhkiField.Description%>"><%=SchetFactura.PlatezhkiField.Name%>:</div>
            <div class="inl al"><cs:DBSDocument id="Platezhki" Width="300px" NextControl="Primechanie" IsMultiSelect="True" IsRemove="True" ConfirmRemove="True" OnChanged="Platezhki_OnChanged" OnDeleted="Platezhki_OnDeleted" runat="server"></cs:DBSDocument></div>
        </div>

       <hr/>
       
       <!--Примечание-->
       <div class="ctlMargMain">
           <div class="inl wd">
             	<div  title="<%=SchetFactura.PrimechanieField.Description%>"><%=SchetFactura.PrimechanieField.Name%>:</div>
		        <span style="FONT-SIZE: 7pt">(для печатных форм)</span>
           </div>
    	    <div class="inl al"><cc:TextArea ID="Primechanie" Height="45px" Width="300px" NextControl="txaDocDesc" MaxLength="500" runat="server"></cc:TextArea></div>
       </div>
       
       <hr/>
       
       <!--Позиции по документу основанию-->
       <div class="ctlMargMain">
           <div style="PADDING-RIGHT:0px; PADDING-LEFT:0px; PADDING-BOTTOM:0px; PADDING-TOP:0px">Позиции по документу основанию:</div>
           <div id="spKurs"></div>
           	<div id="Sales">
				<i>&nbsp;отсутствуют</i>
			</div>
       </div>
       
       <!--Приложение к счету-фактуре-->
       <div class="ctlMargMain">
        <div style="PADDING-RIGHT:0px; PADDING-LEFT:0px; PADDING-BOTTOM:0px; PADDING-TOP:0px">
		  <a href="#" onclick="javascript:cmd('cmd','ShowPril')"><img border="0" alt=" " src="/styles/file.gif"/> Приложение к счету-фактуре</a>
		</div>
		  <div id="FacturaPril"></div>
       </div>

       <br/>
       
       <!--Описание документа в архиве-->
       <% StartRenderVariablePart(Response.Output); %> 
       <% EndRenderVariablePart(Response.Output); %>

       <br/>
       
       <div>
           <% RenderLinkedDocsInfo(Response.Output); %>
       </div>
  </div>
</body>
</html>
