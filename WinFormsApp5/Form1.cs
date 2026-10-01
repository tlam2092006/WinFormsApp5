using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WinFormsApp5
{
    public partial class Form1 : Form
    {
        private List<Product> _allProducts = new List<Product>();
        private BindingList<Product> _viewList = new BindingList<Product>();
        private BindingSource _bs = new BindingSource();
        private string _lastImagePath = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Setup categories
            var categories = new[] {
                new { Name = "Điện thoại", Value = "Điện thoại" },
                new { Name = "Laptop", Value = "Laptop" },
                new { Name = "Phụ kiện", Value = "Phụ kiện" }
            };
            cboCategory.DisplayMember = "Name";
            cboCategory.ValueMember = "Value";
            cboCategory.DataSource = categories;

            // Setup DataGridView columns
            dgvProducts.Columns.Clear();
            dgvProducts.AutoGenerateColumns = false;

            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã SP", DataPropertyName = "ProductId" });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tên SP", DataPropertyName = "ProductName" });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Danh Mục", DataPropertyName = "Category" });
            var priceCol = new DataGridViewTextBoxColumn { HeaderText = "Đơn Giá", DataPropertyName = "UnitPrice" };
            priceCol.DefaultCellStyle.Format = "N0";
            dgvProducts.Columns.Add(priceCol);
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Số Lượng", DataPropertyName = "Quantity" });

            // Binding
            _viewList = new BindingList<Product>(_allProducts);
            _bs.DataSource = _viewList;
            dgvProducts.DataSource = _bs;

            dgvProducts.SelectionChanged += DgvProducts_SelectionChanged;
            dgvProducts.CellFormatting += DgvProducts_CellFormatting;

            txtSearch = new TextBox(); // ensure exists (designer used hidden one)
            // try to find existing search box on form; if designer had one visible, use that
            var existingSearch = this.Controls.Find("txtSearch", true).FirstOrDefault() as TextBox;
            if (existingSearch != null)
            {
                txtSearch = existingSearch;
                txtSearch.TextChanged += TxtSearch_TextChanged;
            }

            UpdateStatus();
        }

        private void DgvProducts_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvProducts.Columns[e.ColumnIndex].DataPropertyName == "UnitPrice" && e.Value != null)
            {
                if (decimal.TryParse(e.Value.ToString(), out var v))
                {
                    e.Value = string.Format("{0:N0} VNĐ", v);
                    e.FormattingApplied = true;
                }
            }
        }

        private void DgvProducts_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvProducts.CurrentRow == null) return;
            var p = dgvProducts.CurrentRow.DataBoundItem as Product;
            if (p == null) return;
            txtProductId.Text = p.ProductId;
            txtProductName.Text = p.ProductName;
            txtUnitPrice.Text = p.UnitPrice.ToString();
            txtQuantity.Text = p.Quantity.ToString();
            cboCategory.SelectedValue = p.Category;
            picAvatar.Image = p.Avatar;
        }

        private void TxtSearch_TextChanged(object? sender, EventArgs e)
        {
            ApplyFilter(txtSearch.Text);
        }

        private void ApplyFilter(string term)
        {
            var filtered = string.IsNullOrWhiteSpace(term)
                ? _allProducts
                : _allProducts.Where(x => x.ProductName != null && x.ProductName.IndexOf(term, StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();
            _viewList = new BindingList<Product>(filtered);
            _bs.DataSource = _viewList;
            dgvProducts.DataSource = _bs;
            UpdateStatus();
        }

        private bool ValidateInputs()
        {
            bool ok = true;
            errorProvider.Clear();
            if (string.IsNullOrWhiteSpace(txtProductName.Text))
            {
                errorProvider.SetError(txtProductName, "Tên SP không được để trống");
                ok = false;
            }
            if (!decimal.TryParse(txtUnitPrice.Text, out var price) || price <= 0)
            {
                errorProvider.SetError(txtUnitPrice, "Đơn giá phải > 0");
                ok = false;
            }
            if (!int.TryParse(txtQuantity.Text, out var qty) || qty < 0)
            {
                errorProvider.SetError(txtQuantity, "Số lượng phải >= 0");
                ok = false;
            }
            return ok;
        }

        private void btnChooseImage_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog();
            ofd.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var img = Image.FromFile(ofd.FileName);
                    picAvatar.Image = img;
                    _lastImagePath = ofd.FileName;
                }
                catch
                {
                    MessageBox.Show("Không thể mở file ảnh.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs()) return;
            var p = new Product
            {
                ProductId = txtProductId.Text.Trim() == string.Empty ? Guid.NewGuid().ToString().Substring(0, 8) : txtProductId.Text.Trim(),
                ProductName = txtProductName.Text.Trim(),
                Category = cboCategory.SelectedValue?.ToString() ?? string.Empty,
                UnitPrice = decimal.Parse(txtUnitPrice.Text),
                Quantity = int.Parse(txtQuantity.Text),
                Avatar = picAvatar.Image == null ? null : new Bitmap(picAvatar.Image)
            };
            _allProducts.Add(p);
            ApplyFilter(txtSearch?.Text ?? string.Empty);
            ClearInputs();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (dgvProducts.CurrentRow == null) return;
            if (!ValidateInputs()) return;
            var current = dgvProducts.CurrentRow.DataBoundItem as Product;
            if (current == null) return;
            current.ProductId = txtProductId.Text.Trim();
            current.ProductName = txtProductName.Text.Trim();
            current.Category = cboCategory.SelectedValue?.ToString() ?? string.Empty;
            current.UnitPrice = decimal.Parse(txtUnitPrice.Text);
            current.Quantity = int.Parse(txtQuantity.Text);
            current.Avatar = picAvatar.Image == null ? null : new Bitmap(picAvatar.Image);
            _bs.ResetBindings(false);
            UpdateStatus();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (dgvProducts.CurrentRow == null) return;
            var current = dgvProducts.CurrentRow.DataBoundItem as Product;
            if (current == null) return;
            var dr = MessageBox.Show("Bạn có chắc muốn xóa sản phẩm đã chọn?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr == DialogResult.Yes)
            {
                _allProducts.Remove(current);
                ApplyFilter(txtSearch?.Text ?? string.Empty);
            }
        }

        private void ClearInputs()
        {
            txtProductId.Text = string.Empty;
            txtProductName.Text = string.Empty;
            txtUnitPrice.Text = string.Empty;
            txtQuantity.Text = string.Empty;
            cboCategory.SelectedIndex = 0;
            picAvatar.Image = null;
            errorProvider.Clear();
        }

        private void UpdateStatus()
        {
            toolStripStatusLabelCount.Text = $"Tổng số sản phẩm: {_allProducts.Count}";
        }

        private void exportCsvToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog();
            sfd.Filter = "CSV Files|*.csv";
            sfd.DefaultExt = "csv";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("ProductId,ProductName,Category,UnitPrice,Quantity");
                    foreach (var p in _allProducts)
                    {
                        var line = string.Format("\"{0}\",\"{1}\",\"{2}\",{3},{4}",
                            p.ProductId, p.ProductName?.Replace("\"", "\"\""), p.Category, p.UnitPrice, p.Quantity);
                        sb.AppendLine(line);
                    }
                    File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show("Xuất CSV thành công.", "Xuất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Xuất CSV thất bại: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }

    // Note: Product class is defined in separate file Product.cs
}
