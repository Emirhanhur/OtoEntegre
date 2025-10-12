using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OtoEntegre.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTedarikUrunlerTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_platformlar_platform_turleri_TipId",
                table: "platformlar");

            migrationBuilder.DropForeignKey(
                name: "FK_siparisler_kullanicilar_kullanici_id",
                table: "siparisler");

            migrationBuilder.DropTable(
                name: "siparis_dosyalari");

            migrationBuilder.DropIndex(
                name: "IX_siparisler_kullanici_id",
                table: "siparisler");

            migrationBuilder.DropIndex(
                name: "IX_platformlar_TipId",
                table: "platformlar");

            migrationBuilder.DropIndex(
                name: "IX_dealers_Email_Phone",
                table: "dealers");

            migrationBuilder.DropColumn(
                name: "TipId",
                table: "platformlar");

            migrationBuilder.DropColumn(
                name: "Balance",
                table: "dealers");

            migrationBuilder.DropColumn(
                name: "Discount",
                table: "dealers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "dealers");

            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "dealers",
                newName: "phone");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "dealers",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Lastname",
                table: "dealers",
                newName: "lastname");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "dealers",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "dealers",
                newName: "id");

            migrationBuilder.AddColumn<string>(
                name: "urun_tedarik_barcode",
                table: "urunler",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "dealer_id",
                table: "siparisler",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "kod",
                table: "siparisler",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "tedarik_kullanici_id",
                table: "siparisler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "tedarik_sent",
                table: "siparisler",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "telegram_sent",
                table: "siparisler",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "platformlar",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "tip_id",
                table: "platformlar",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "platformlar",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "api_url",
                table: "platformlar",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "entegrasyon_telefon",
                table: "kullanicilar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tedarik_kullanici_id",
                table: "kullanicilar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "seller_id",
                table: "entegrasyonlar",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tedarik_urunler",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    barcode = table.Column<string>(type: "text", nullable: false),
                    brand = table.Column<string>(type: "text", nullable: false),
                    category1 = table.Column<string>(type: "text", nullable: false),
                    category2 = table.Column<string>(type: "text", nullable: false),
                    category3 = table.Column<string>(type: "text", nullable: false),
                    category4 = table.Column<string>(type: "text", nullable: false),
                    sale_price = table.Column<decimal>(type: "numeric", nullable: false),
                    buy_price = table.Column<decimal>(type: "numeric", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    tax_rate = table.Column<int>(type: "integer", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_sync_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tedarik_urunler", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_siparisler_dealer_id",
                table: "siparisler",
                column: "dealer_id");

            migrationBuilder.CreateIndex(
                name: "IX_platformlar_tip_id",
                table: "platformlar",
                column: "tip_id");

            migrationBuilder.AddForeignKey(
                name: "FK_platformlar_platform_turleri_tip_id",
                table: "platformlar",
                column: "tip_id",
                principalTable: "platform_turleri",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_siparisler_dealers_dealer_id",
                table: "siparisler",
                column: "dealer_id",
                principalTable: "dealers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_platformlar_platform_turleri_tip_id",
                table: "platformlar");

            migrationBuilder.DropForeignKey(
                name: "FK_siparisler_dealers_dealer_id",
                table: "siparisler");

            migrationBuilder.DropTable(
                name: "tedarik_urunler");

            migrationBuilder.DropIndex(
                name: "IX_siparisler_dealer_id",
                table: "siparisler");

            migrationBuilder.DropIndex(
                name: "IX_platformlar_tip_id",
                table: "platformlar");

            migrationBuilder.DropColumn(
                name: "urun_tedarik_barcode",
                table: "urunler");

            migrationBuilder.DropColumn(
                name: "dealer_id",
                table: "siparisler");

            migrationBuilder.DropColumn(
                name: "kod",
                table: "siparisler");

            migrationBuilder.DropColumn(
                name: "tedarik_kullanici_id",
                table: "siparisler");

            migrationBuilder.DropColumn(
                name: "tedarik_sent",
                table: "siparisler");

            migrationBuilder.DropColumn(
                name: "telegram_sent",
                table: "siparisler");

            migrationBuilder.DropColumn(
                name: "entegrasyon_telefon",
                table: "kullanicilar");

            migrationBuilder.DropColumn(
                name: "tedarik_kullanici_id",
                table: "kullanicilar");

            migrationBuilder.DropColumn(
                name: "seller_id",
                table: "entegrasyonlar");

            migrationBuilder.RenameColumn(
                name: "phone",
                table: "dealers",
                newName: "Phone");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "dealers",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "lastname",
                table: "dealers",
                newName: "Lastname");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "dealers",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "dealers",
                newName: "Id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "platformlar",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "tip_id",
                table: "platformlar",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "platformlar",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "api_url",
                table: "platformlar",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TipId",
                table: "platformlar",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Balance",
                table: "dealers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Discount",
                table: "dealers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "dealers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "siparis_dosyalari",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    SiparisId = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    dosya_turu = table.Column<string>(type: "text", nullable: false),
                    dosya_url = table.Column<string>(type: "text", nullable: false),
                    siparis_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_siparis_dosyalari", x => x.id);
                    table.ForeignKey(
                        name: "FK_siparis_dosyalari_siparisler_SiparisId",
                        column: x => x.SiparisId,
                        principalTable: "siparisler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_siparisler_kullanici_id",
                table: "siparisler",
                column: "kullanici_id");

            migrationBuilder.CreateIndex(
                name: "IX_platformlar_TipId",
                table: "platformlar",
                column: "TipId");

            migrationBuilder.CreateIndex(
                name: "IX_dealers_Email_Phone",
                table: "dealers",
                columns: new[] { "Email", "Phone" });

            migrationBuilder.CreateIndex(
                name: "IX_siparis_dosyalari_SiparisId",
                table: "siparis_dosyalari",
                column: "SiparisId");

            migrationBuilder.AddForeignKey(
                name: "FK_platformlar_platform_turleri_TipId",
                table: "platformlar",
                column: "TipId",
                principalTable: "platform_turleri",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_siparisler_kullanicilar_kullanici_id",
                table: "siparisler",
                column: "kullanici_id",
                principalTable: "kullanicilar",
                principalColumn: "id");
        }
    }
}
