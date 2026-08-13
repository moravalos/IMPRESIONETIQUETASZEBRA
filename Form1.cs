using MiniExcelLibs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZXing;
using static System.Net.WebRequestMethods;
using File = System.IO.File;

namespace ALMACENGRAL
{
    public partial class Form1 : Form
    {
        private string nombreImpresora = "";



        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }


        private void button1_Click(object sender, EventArgs e)
        {
            nombreImpresora = combobox.Text;
            

            if (string.IsNullOrEmpty(nombreImpresora))
            {
                MessageBox.Show("No hay una impresora Zebra lista. Conéctala e intenta de nuevo.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            // 1. Capturar los datos de la interfaz
            string producto = txtnoparte.Text;
            string codigo1 = txt1.Text;
            string codigo2 = txt2.Text;
            string codigo3 = txt3.Text;
            string codigo4 = txt4.Text;


            // Validar que no estén vacíos
            if (string.IsNullOrEmpty(producto) || string.IsNullOrEmpty(codigo1) || string.IsNullOrEmpty(codigo2) || string.IsNullOrEmpty(codigo3) || string.IsNullOrEmpty(codigo4) || string.IsNullOrEmpty(nombreImpresora))
            {
                MessageBox.Show("Por favor, llena todos los campos.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. Estructura de tu código ZPL con marcadores de posición
            // Reemplaza los valores fijos por tus variables usando String Interpolation ($)
            //string codigoZPL = $@"";
            string ZPL = $@"^XA
~TA000
~JSN
^LT0
^MNW
^MTT
^PON
^PMN
^LH0,0
^JMA
^PR6,6
~SD15
^JUS
^LRN
^CI27
^PA0,1,1,0
^XZ
^XA
^MMT
^PW650
^LL295
^LS0
^FT248,83^A0N,46,46^FH\^CI28^FD{producto}^FS^CI27
^FT226,141^A0N,25,20^FH\^CI28^FD{codigo1}^FS^CI27
^FT226,183^A0N,25,20^FH\^CI28^FD{codigo2}^FS^CI27
^FT226,219^A0N,21,20^FH\^CI28^FD{codigo3}^FS^CI27
^FT9,280^BQN,2,7
^FH\^FDLA,{codigo4}^FS
^PQ1,0,1,Y
^XZ";

            // 3. Enviar el código estructurado a la impresora
            try
            {
                bool exito = RawPrinterHelper.SendStringToPrinter(nombreImpresora, ZPL);

                if (exito)
                {
                    MessageBox.Show("Etiqueta enviada correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    // Limpiar campos después de imprimir
                }
                else
                {
                    MessageBox.Show("No se pudo enviar la impresión. Revisa la conexión.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error: {ex.Message}", "Error Técnico", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            txtnoparte.Clear();
            txt3.Clear();   
            txt2.Clear();  
            txt1.Clear();
            txt4.Clear();
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Archivos de Excel (*.xlsx)|*.xlsx|Todos los archivos (*.*)|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string rutaArchivo = openFileDialog.FileName;

                        // Lee el archivo de Excel y lo convierte en un DataTable
                        DataTable dt = MiniExcel.QueryAsDataTable(rutaArchivo);

                        // Asigna los datos al DataGridView de la interfaz
                        dgvDatos.DataSource = dt;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al leer el archivo Excel: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            nombreImpresora = combobox.Text;

            if (string.IsNullOrEmpty(nombreImpresora))
            {
                MessageBox.Show("No hay una impresora Zebra lista. Conéctala e intenta de nuevo.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            // 2. Validar que la tabla tenga datos cargados
            if (dgvDatos.Rows.Count == 0 || (dgvDatos.Rows.Count == 1 && dgvDatos.Rows[0].IsNewRow))
            {
                MessageBox.Show("No hay datos en la tabla para imprimir. Carga un archivo Excel primero.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int etiquetasEnviadas = 0;
            int filasOmitidas = 0;

            // Mensaje de confirmación antes de iniciar una impresión masiva
            DialogResult resultado = MessageBox.Show($"¿Estás seguro de que deseas imprimir las {dgvDatos.Rows.Count} etiquetas de la tabla?",
                                                     "Confirmar Impresión Masiva", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (resultado != DialogResult.Yes) return;

            try
            {
                // 3. Recorrer cada una de las filas del DataGridView
                foreach (DataGridViewRow fila in dgvDatos.Rows)
                {
                    // Omitir la fila en blanco que añade automáticamente el DataGridView al final
                    if (fila.IsNewRow) continue;

                    // Extraer los datos de las celdas de la fila actual
                    // RECUERDA: "Producto" y "Codigo" deben ser los nombres EXACTOS de tus columnas de Excel
                    string producto = fila.Cells["UID"].Value?.ToString();
                    string codigo1 = fila.Cells["Cont_Code"].Value?.ToString();
                    string codigo2 = fila.Cells["Referencia (Loc_Code · Tipo · Nivel)"].Value?.ToString();
                    string codigo3 = fila.Cells["Contenido / Rango"].Value?.ToString();
                    string codigo4 = fila.Cells["Etiqueta_física (texto QR - parte 1)"].Value?.ToString();


                    // Si alguna fila viene vacía o incompleta, la saltamos para no trabar el ciclo
                    if (string.IsNullOrEmpty(producto) || string.IsNullOrEmpty(codigo1) || string.IsNullOrEmpty(codigo2) || string.IsNullOrEmpty(codigo3) || string.IsNullOrEmpty(nombreImpresora))
                    {
                        filasOmitidas++;
                        continue;
                    }

                    // 4. Armar el formato ZPL individual para este registro
                    //string codigoZPL = $@"";

                     string ZPL = $@"^XA
~TA000
~JSN
^LT0
^MNW
^MTT
^PON
^PMN
^LH0,0
^JMA
^PR6,6
~SD15
^JUS
^LRN
^CI27
^PA0,1,1,0
^XZ
^XA
^MMT
^PW650
^LL295
^LS0
^FT248,83^A0N,46,46^FH\^CI28^FD{producto}^FS^CI27
^FT226,141^A0N,25,20^FH\^CI28^FD{codigo1}^FS^CI27
^FT226,183^A0N,25,20^FH\^CI28^FD{codigo2}^FS^CI27
^FT226,219^A0N,21,20^FH\^CI28^FD{codigo3}^FS^CI27
^FT9,280^BQN,2,7
^FH\^FDLA,{codigo4}^FS
^PQ1,0,1,Y
^XZ";


        // 5. Enviar el ZPL de este producto a la impresora
        bool exito = RawPrinterHelper.SendStringToPrinter(nombreImpresora, ZPL);

                    if (exito)
                    {
                        etiquetasEnviadas++;
                    }
                }

                // 6. Resumen de la operación al finalizar el ciclo
                string mensajeFinal = $"Proceso terminado.\n\n• Etiquetas impresas: {etiquetasEnviadas}";
                if (filasOmitidas > 0)
                {
                    mensajeFinal += $"\n• Filas omitidas por datos incompletos: {filasOmitidas}";
                }

                MessageBox.Show(mensajeFinal, "Impresión Masiva Completada", MessageBoxButtons.OK, MessageBoxIcon.Information);

                dgvDatos.DataSource = null;

                // Limpiar filas y columnas visuales por completo
                dgvDatos.Rows.Clear();
                dgvDatos.Columns.Clear();

                // Forzar la liberación de memoria para cerrar el archivo Excel en segundo plano
                GC.Collect();
                GC.WaitForPendingFinalizers();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un problema durante la impresión masiva: {ex.Message}", "Error Técnico", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            dgvDatos.DataSource = null;

            // Limpiar filas y columnas visuales por completo
            dgvDatos.Rows.Clear();
            dgvDatos.Columns.Clear();

            // Forzar la liberación de memoria para cerrar el archivo Excel en segundo plano
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void button5_Click(object sender, EventArgs e)
        {
           
        }
    }
    }

