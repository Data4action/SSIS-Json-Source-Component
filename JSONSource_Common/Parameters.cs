﻿using Microsoft.SqlServer.Dts.Runtime;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
#if LINQ_SUPPORTED
using System.Linq;
#endif


namespace com.webkingsoft.JSONSource_Common
{
    public partial class Parameters : Form
    {
        private List<HTTPParameter> _model;
        private Microsoft.SqlServer.Dts.Runtime.Variables _vars;

        public Parameters(Microsoft.SqlServer.Dts.Runtime.Variables vars)
        {
            _vars = vars;
            InitializeComponent();
            bindingType.DataSource = Enum.GetNames(typeof(HTTPParamBinding));
            dataGridView1.CellBeginEdit += dataGridView1_CellBeginEdit;
            dataGridView1.CellValueChanged += dataGridView1_CellValueChanged;
            dataGridView1.RowValidating += dataGridView1_RowValidating;
            _model = new List<HTTPParameter>();
            
        }

        void dataGridView1_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            // Convalida la riga.
            DataGridView d = (DataGridView)sender;
            DataGridViewCell name = d.Rows[e.RowIndex].Cells[0];
            DataGridViewCell binding = d.Rows[e.RowIndex].Cells[1];
            DataGridViewCell value = d.Rows[e.RowIndex].Cells[2];
            DataGridViewCell encode = d.Rows[e.RowIndex].Cells[3];

            // 1. Nome del parametro non nullo
            if (name.Value==null || String.IsNullOrEmpty(name.Value.ToString().Trim()))
            {
                d.Rows[e.RowIndex].Cells[0].ErrorText= "Parameter name cannot be null or empty.";
                e.Cancel = true;
                return;
            }

            // Controlla che il nome del parametro sia univoco
            foreach (DataGridViewRow r in d.Rows) {
                if (r.Cells[0].Value == null)
                    // E' una riga appena creata, skip!
                    continue;
                if (r.Cells[0].Value.ToString().Trim() == name.Value.ToString().Trim() && !Object.ReferenceEquals(r,d.Rows[e.RowIndex])) { 
                    // Duplicato!
                    d.Rows[e.RowIndex].Cells[0].ErrorText = "Duplicate Parameter name detected.";
                    e.Cancel = true;
                    return;
                }
            }

            // 2. Tipo di binding
            HTTPParamBinding bin;
            if (!Enum.TryParse<HTTPParamBinding>(binding.Value.ToString(), out bin)) {
                d.Rows[e.RowIndex].Cells[1].ErrorText = "Invalid binding option specified.";
                e.Cancel = true;
                return;
            }

            // 3. Valore: se è di tipo bound, controlla che la variabile specificata esista. Se no, accetta tutto. Per noi un parametro HTTP può anche essere nullo.
            if (bin == HTTPParamBinding.Variable) {
                string var = value.Value.ToString().Trim();
                bool valid = false;
                foreach (Variable v in _vars) {
                    if (v.QualifiedName == var) {
                        valid = true;
                        break;
                    }
                }
                if (!valid)
                {
                    d.Rows[e.RowIndex].Cells[2].ErrorText = "Invalid variable choosen";
                    e.Cancel = true;
                    return;
                }
            }
        }

        

        void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 1) {
                DataGridView d = (DataGridView)sender;
                // Se è cambiatoil tipo di binding, occorre resettare il valore del valore associato.
                d.Rows[e.RowIndex].Cells[2].Value = null;
            }
        }

        void dataGridView1_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            DataGridView d = (DataGridView)sender;
            // Se si sta per modificare il valore del parametro...
            if (e.ColumnIndex == 2) {
                HTTPParamBinding b = (HTTPParamBinding)Enum.Parse(typeof(HTTPParamBinding), d.Rows[e.RowIndex].Cells[1].Value.ToString());
                // Se la riga corrente si riferisce ad un parametro variable-bound...
                if (b == HTTPParamBinding.Variable) { 
                    // Mostra il dialog di scelta delle variabili
                    VariableChooser vc = new VariableChooser(_vars);
                    DialogResult dr = vc.ShowDialog();
                    if (dr == DialogResult.OK)
                    {
                        Microsoft.SqlServer.Dts.Runtime.Variable v = vc.GetResult();
                        d.Rows[e.RowIndex].Cells[2].Value = v.QualifiedName;
                        e.Cancel = true;
                        //d.EndEdit();
                    }
                }
            }
        }


        public IEnumerable<HTTPParameter> GetModel() {
            return _model;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int r = dataGridView1.Rows.Add();
            dataGridView1.Rows[r].Cells[0].Value = null;
            dataGridView1.Rows[r].Cells[1].Value = Enum.GetName(typeof(HTTPParamBinding),HTTPParamBinding.CustomValue);
            dataGridView1.Rows[r].Cells[2].Value = null;
            dataGridView1.Rows[r].Cells[3].Value = true;
            dataGridView1.CurrentCell = dataGridView1.Rows[r].Cells[0];
            dataGridView1.BeginEdit(true);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Prepare model
            _model.Clear();
            foreach (DataGridViewRow row in dataGridView1.Rows) {
                HTTPParameter p = new HTTPParameter();
                p.Name = row.Cells[0].Value.ToString().Trim();
                p.Binding = (HTTPParamBinding) Enum.Parse(typeof(HTTPParamBinding), row.Cells[1].Value.ToString().Trim());
                p.Value = row.Cells[2].Value.ToString().Trim();
                p.Encode = (bool)row.Cells[3].Value;
                _model.Add(p);
            }

            DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        public void SetModel(IEnumerable<HTTPParameter> pars) {
            _model.Clear();
            dataGridView1.Rows.Clear();
            if (pars!=null)
                foreach (HTTPParameter p in pars) {
                    dataGridView1.Rows.Add(new object[] { p.Name, Enum.GetName(typeof(HTTPParamBinding),p.Binding), p.Value, p.Encode });
                    _model.Add(p);
                }
        }

        private void delButton_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count < 1)
            {
                MessageBox.Show("No row has been selected.");
                return;
            }
            else {
                for (int i=0;i<dataGridView1.SelectedRows.Count;i++) {
                    dataGridView1.Rows.Remove(dataGridView1.SelectedRows[i]);
                }
            }
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            DataGridView v = (DataGridView)sender;
            delButton.Enabled = v.SelectedRows.Count > 0;
        }
    }
}
