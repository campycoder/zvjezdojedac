﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Alati;
using Prototip.Igra.Brodovi.Dizajner;

namespace Prototip
{
	public partial class FormFlote : Form
	{
		private enum InfoStranice
		{
			PrimarnaMisija = 0,
			SekundarnaMisija,
			Stit,
			Reaktor,
			Pokretljivost,
			Senzori,
			MZPogon,
			SpecijalnaOprema,
			Taktika
		}

		private const string SeparatorNDGrupa = "-----";
		private Dictionary<InfoStranice, string> nazivInfoStranice = new Dictionary<InfoStranice, string>();
		private Igrac igrac;
		private Dizajner dizajner;
		private InfoStranice prethodniNDinfo = InfoStranice.PrimarnaMisija;
		private int prethodnaNDprimMisija = 0;
		private int prethodnaNDsekMisija = 0;
		private bool zadrziNDInfo = false;
		private SpecijalnaOprema specijalnaOpremaZaOpis = null;

		public FormFlote(Igrac igrac)
		{
			InitializeComponent();
			btnNDZadrziInfo.Text = "";
			lstvDizajnovi.SmallImageList = new ImageList();
			lstvDizajnovi.SmallImageList.ImageSize = new Size(60, 40);
			this.igrac = igrac;

			nazivInfoStranice.Add(InfoStranice.MZPogon, "MZ pogon");
			nazivInfoStranice.Add(InfoStranice.Pokretljivost, "Pokretljivost");
			nazivInfoStranice.Add(InfoStranice.PrimarnaMisija, "Prim. misija");
			nazivInfoStranice.Add(InfoStranice.Reaktor, "Reaktor");
			nazivInfoStranice.Add(InfoStranice.SekundarnaMisija, "Sek. misija");
			nazivInfoStranice.Add(InfoStranice.Senzori, "Senzori");
			nazivInfoStranice.Add(InfoStranice.SpecijalnaOprema, "Spec. oprema");
			nazivInfoStranice.Add(InfoStranice.Stit, "Štit");
			nazivInfoStranice.Add(InfoStranice.Taktika, "Taktika");

			#region Dizajnovi
			{
				foreach (DizajnZgrada dizajnZgrada in igrac.dizajnoviBrodova) {
					Dizajn dizajn = dizajnZgrada.dizajn;
					dodajDizajn(dizajn);
				}
			}
			#endregion

			#region Novi dizajn
			{
				dizajner = new Dizajner(igrac);

				foreach (Trup trup in dizajner.trupovi)
					cbNDvelicina.Items.Add(trup);

				cbNDprimMisija.Items.Add(new TagTekst<Oruzje>(null, "ništa"));
				cbNDsekMisija.Items.Add(new TagTekst<Oruzje>(null, "ništa"));
				foreach (Misija.Tip misija in dizajner.oruzja.Keys) {
					if (dizajner.oruzja[misija].Count == 0) continue;
					cbNDprimMisija.Items.Add(new TagTekst<Oruzje>(null, SeparatorNDGrupa));
					cbNDsekMisija.Items.Add(new TagTekst<Oruzje>(null, SeparatorNDGrupa));

					foreach (Oruzje oruzje in dizajner.oruzja[misija]) {
						cbNDprimMisija.Items.Add(new TagTekst<Oruzje>(oruzje, oruzje.info.naziv));
						cbNDsekMisija.Items.Add(new TagTekst<Oruzje>(oruzje, oruzje.info.naziv));
					}
				}

				cbNDstit.Items.Add(new TagTekst<int>(-1, "nikakav"));
				int i = 0;
				foreach (Stit stit in dizajner.trupKomponente.stitovi) {
					cbNDstit.Items.Add(new TagTekst<int>(i, stit.info.naziv));
					i++;
				}

				foreach (Taktika taktika in Taktika.Taktike.Keys)
					cbNDtaktika.Items.Add(new TagTekst<Taktika>(taktika, taktika.naziv));

				i = 0;
				foreach (SpecijalnaOprema so in dizajner.trupKomponente.specijalnaOprema) {
					ListViewItem item = new ListViewItem("");
					item.SubItems.Add(so.naziv);
					item.SubItems.Add(so.velicina.ToString());
					item.Tag = i;
					lstvNDspecOprema.Items.Add(item);
					i++;
				}

				foreach (InfoStranice strana in Enum.GetValues(typeof(InfoStranice)))
					cbNDinfoStrana.Items.Add(new TagTekst<InfoStranice>(strana, nazivInfoStranice[strana]));

				cbNDvelicina.SelectedIndex = 0;
				cbNDprimMisija.SelectedIndex = 0;
				cbNDsekMisija.SelectedIndex = 0;
				cbNDstit.SelectedIndex = 0;
				cbNDtaktika.SelectedIndex = 0;
				hscrUdioMisija.Value = 33;
			}
			#endregion
		}

		private T izvadiTag<T>(ComboBox cb)
		{
			return ((TagTekst<T>)cb.SelectedItem).tag;
		}

		private List<string> opisOruzja(bool primarno, Dizajn.Zbir<Oruzje> oruzje, bool cijene)
		{
			List<string> opis = new List<string>();

			if (primarno) opis.Add("Primarna misija: ");
			else opis.Add("Sekundarna misija: ");

			if (oruzje == null) {
				opis.Add("");
				if (primarno) opis.Add("Brod nema primarnu misiju.");
				else opis.Add("Brod nema sekundarnu misiju");

				return opis;
			}

			Misija.Tip misijaTip = oruzje.komponenta.misija;
			Misija misija = Misija.Opisnici[misijaTip];

			opis.Add(misija.naziv);
			opis.Add((misija.grupirana) ?
				oruzje.komponenta.naziv :
				Fje.PrefiksFormater(oruzje.kolicina) + " x " + oruzje.komponenta.naziv);
			opis.Add("");
			if (oruzje.komponenta.maxNivo > 0) opis.Add("Nivo: " + oruzje.komponenta.nivo);
			if (misija.imaCiljanje) opis.Add("Ciljanje: " + Oruzje.OruzjeInfo.OpisCiljanja[oruzje.komponenta.ciljanje]);

			for (int i = 0; i < misija.brParametara; i++) {
				double vrijednost = oruzje.komponenta.parametri[i];
				if (misija.parametri[i].mnoziKolicinom)
					vrijednost *= oruzje.kolicina;
				switch (misija.parametri[i].tip) {
					case Misija.TipParameta.Cijelobrojni:
						opis.Add(misija.parametri[i].opis + ": " + Fje.PrefiksFormater(vrijednost));
						break;
					case Misija.TipParameta.Postotak:
						opis.Add(misija.parametri[i].opis + ": " + vrijednost.ToString("0.##"));
						break;
				}
			}

			if (cijene) {
				opis.Add("");
				opis.Add("Potrebna snaga: " + Fje.PrefiksFormater(oruzje.komponenta.snaga));
				opis.Add("Cijena: " + Fje.PrefiksFormater(oruzje.komponenta.cijena * oruzje.kolicina));
			}

			return opis;
		}

		private List<string> opis(InfoStranice stranica, Dizajn dizajn)
		{
			return opis(stranica, dizajn, true);
		}

		private List<string> opis(InfoStranice stranica, Dizajn dizajn, bool cijene)
		{
			List<string> opis = new List<string>();
			switch (stranica) {
				case InfoStranice.MZPogon:
					opis.Add("MZ pogon");
					opis.Add("");
					if (dizajn.MZPogon == null)
						opis.Add("Brod nema međuzvjezdani pogon.");
					else {
						opis.Add(dizajn.MZPogon.info.naziv);
						if (dizajn.MZPogon.maxNivo > 0)
							opis.Add("Nivo: " + dizajn.MZPogon.nivo);
						opis.Add("Brzina: " + dizajn.MZbrzina.ToString("0.###"));
						if (cijene) {
							opis.Add("Potrebna snaga: " + Fje.PrefiksFormater(dizajn.MZPogon.snaga));
							opis.Add("Cijena: " + Fje.PrefiksFormater(dizajn.MZPogon.cijena));
						}
					}
					break;

				case InfoStranice.Pokretljivost:
					opis.Add("Pokretljivost");
					opis.Add("");
					opis.Add(dizajn.potisnici.naziv);
					if (dizajn.potisnici.maxNivo > 0)
						opis.Add("Nivo: " + dizajn.potisnici.nivo);
					opis.Add("Stupanj tromosti: " + dizajn.inercija);
					opis.Add("Pokretljivost: " + Fje.PrefiksFormater(dizajn.pokretljivost));
					break;

				case InfoStranice.PrimarnaMisija:
					opis = opisOruzja(true, dizajn.primarnoOruzje, cijene);
					break;

				case InfoStranice.Reaktor:
					opis.Add("Reaktor");
					opis.Add("");
					opis.Add(dizajn.reaktor.naziv);
					if (dizajn.reaktor.maxNivo > 0)
						opis.Add("Nivo: " + dizajn.reaktor.nivo);
					opis.Add("Opterečenje: " + Fje.PrefiksFormater(dizajn.opterecenjeReaktora));
					opis.Add("Dostupna snaga: " + Fje.PrefiksFormater(dizajn.snagaReaktora) + " (" + (dizajn.koefSnageReaktora * 100).ToString("0") + "%)");
					break;

				case InfoStranice.SekundarnaMisija:
					opis = opisOruzja(false, dizajn.sekundarnoOruzje, cijene);
					break;

				case InfoStranice.Senzori:
					opis.Add("Senzori i prikrivanje");
					opis.Add("");
					opis.Add(Fje.PrefiksFormater(dizajn.brSenzora) + " x " + dizajn.senzor.naziv);
					if (dizajn.senzor.maxNivo > 0)
						opis.Add("Nivo: " + dizajn.senzor.nivo);
					opis.Add("Snaga senzora: " + Fje.PrefiksFormater(dizajn.snagaSenzora));
					opis.Add("Ometanje: " + Fje.PrefiksFormater(dizajn.ometanje));
					opis.Add("Prikrivanje: " + Fje.PrefiksFormater(dizajn.prikrivenost));
					break;

				case InfoStranice.SpecijalnaOprema:
					opis.Add("Specijalna oprema");
					opis.Add("");
					if (specijalnaOpremaZaOpis != null) {
						if (dizajn.specijalnaOprema.ContainsKey(specijalnaOpremaZaOpis))
							opis.Add(dizajn.specijalnaOprema[specijalnaOpremaZaOpis] + " x " + specijalnaOpremaZaOpis.naziv);
						else
							opis.Add(specijalnaOpremaZaOpis.naziv);
						if (specijalnaOpremaZaOpis.maxNivo > 0)
							opis.Add("Nivo: " + specijalnaOpremaZaOpis.nivo);
						opis.AddRange(specijalnaOpremaZaOpis.opisEfekata);
						if (cijene) {
							opis.Add("");
							opis.Add("Veličina: " + Fje.PrefiksFormater(specijalnaOpremaZaOpis.velicina));
							opis.Add("Cijena: " + Fje.PrefiksFormater(specijalnaOpremaZaOpis.cijena));
						}
					}

					break;

				case InfoStranice.Stit:
					opis.Add("Štit");
					opis.Add("");
					if (dizajn.stit == null)
						opis.Add("Brod nema štit.");
					else {
						opis.Add(dizajn.stit.naziv);
						if (dizajn.stit.maxNivo > 0)
							opis.Add("Nivo: " + dizajn.stit.nivo);
						opis.Add("Izdržljivost: " + Fje.PrefiksFormater(dizajn.stit.izdrzljivost));
						opis.Add("Brzina obnavljanja: " + Fje.PrefiksFormater(dizajn.stit.obnavljanje));
						opis.Add("Debljina: " + Fje.PrefiksFormater(dizajn.stit.debljina));
						opis.Add("Ometanje: x" + dizajn.stit.ometanje.ToString("0.##"));
						opis.Add("Prikrivanje: +" + Fje.PrefiksFormater(dizajn.stit.prikrivanje));
						if (cijene) {
							opis.Add("");
							opis.Add("Potrebna snaga: " + Fje.PrefiksFormater(dizajn.stit.snaga));
							opis.Add("Cijena: " + Fje.PrefiksFormater(dizajn.stit.cijena));
						}
					}
					break;

				case InfoStranice.Taktika:
					opis.Add("Taktika");
					opis.Add("");
					opis.Add(dizajn.taktika.naziv);
					opis.Add("Preciznost: x" + dizajn.taktika.koefPreciznost);
					opis.Add("Snaga senzora: x" + dizajn.taktika.koefSnagaSenzora);
					opis.Add("Ometanje: x" + dizajn.taktika.koefOmetanje);
					opis.Add("Prikrivanje: x" + dizajn.taktika.koefPrikrivanje);
					break;
			}

			return opis;
		}

		#region Novi dizajn
		private void ispisiOpis(InfoStranice stranica, Dizajn dizajn)
		{
			txtNDinfo.Lines = opis(stranica, dizajn).ToArray();
			prethodniNDinfo = stranica;
		}

		private void osvjeziNDstatistike()
		{
			Dizajn dizajn = dizajner.dizajn;

			lblNDnosivost.Text = "Nosivost: " + Fje.PrefiksFormater(dizajner.odabranTrup.nosivost);
			lblNDoklop.Text = "Izdržljivost okolopa (" + dizajn.oklop.naziv + "): " + Fje.PrefiksFormater(dizajn.izdrzljivostOklopa);
			lblNDpokretljivost.Text = "Pokretljivost (" + dizajn.potisnici.naziv + "): " + Fje.PrefiksFormater(dizajn.pokretljivost);
			lblNDsenzori.Text = "Snaga senzora (" + dizajn.senzor.naziv + "): " + Fje.PrefiksFormater(dizajn.snagaSenzora);
			picNDSlika.Image = dizajner.odabranTrup.slika;
			lblNDcijena.Text = "Cijena: " + Fje.PrefiksFormater(dizajn.cijena);

			if (dizajn.primarnoOruzje != null)
				cbNDprimMisija.Items[cbNDprimMisija.SelectedIndex] = new TagTekst<Oruzje>(dizajn.primarnoOruzje.komponenta, Fje.PrefiksFormater(dizajn.primarnoOruzje.kolicina) + " x " + dizajn.primarnoOruzje.komponenta.naziv);
			else if (cbNDprimMisija.SelectedItem != null) {
				TagTekst<Oruzje> tagOruzje = (TagTekst<Oruzje>)cbNDprimMisija.SelectedItem;
				if (tagOruzje.tag != null) {
					tagOruzje.tekst = tagOruzje.tag.naziv;
					cbNDprimMisija.Items[cbNDprimMisija.SelectedIndex] = tagOruzje;
				}
			}

			if (dizajn.sekundarnoOruzje != null)
				cbNDsekMisija.Items[cbNDsekMisija.SelectedIndex] = new TagTekst<Oruzje>(dizajn.sekundarnoOruzje.komponenta, Fje.PrefiksFormater(dizajn.sekundarnoOruzje.kolicina) + " x " + dizajn.sekundarnoOruzje.komponenta.naziv);
			else if (cbNDsekMisija.SelectedItem != null) {
				TagTekst<Oruzje> tagOruzje = (TagTekst<Oruzje>)cbNDsekMisija.SelectedItem;
				if (tagOruzje.tag != null) {
					tagOruzje.tekst = tagOruzje.tag.naziv;
					cbNDsekMisija.Items[cbNDsekMisija.SelectedIndex] = tagOruzje;
				}
			}

			lblNDslobodno.Text = "Slobodan prostor: " + Fje.PrefiksFormater(dizajner.slobodnaNosivost);
			ispisiOpis(prethodniNDinfo, dizajn);
			provjeriDizajn();
		}

		private void provjeriDizajn()
		{
			Dizajn dizajn = dizajner.dizajn;
			bool valid = true;

			if (dizajner.slobodnaNosivost < 0) valid = false;
			if (dizajn.ime == null) valid = false;
			else if (dizajn.ime.Trim().Length == 0) valid = false;

			if (!valid && btnSpremi.Enabled == true) btnSpremi.Enabled = false;
			if (valid && btnSpremi.Enabled == false) btnSpremi.Enabled = true;
		}

		private void prebaciNDopis(InfoStranice stranica)
		{
			if (zadrziNDInfo) {
				ispisiOpis(prethodniNDinfo, dizajner.dizajn);
				return;
			}

			if (cbNDinfoStrana.SelectedIndex == (int)stranica)
				ispisiOpis(stranica, dizajner.dizajn);
			else
				cbNDinfoStrana.SelectedIndex = (int)stranica;

			prethodniNDinfo = stranica;
		}

		private void cbNDvelicina_SelectedIndexChanged(object sender, EventArgs e)
		{
			Trup trup = (Trup)cbNDvelicina.SelectedItem;
			dizajner.odabranTrup = trup;

			if (dizajner.komponente[trup].mzPogon == null) {
				chNDMZpogon.Checked = false;
				chNDMZpogon.Enabled = false;
			}
			else
				chNDMZpogon.Enabled = true;

			foreach (ListViewItem item in lstvNDspecOprema.Items) {
				int indeks = (int)item.Tag;
				item.SubItems[2].Text = dizajner.trupKomponente.specijalnaOprema[indeks].velicina.ToString();
			}

			if (lstvNDspecOprema.SelectedItems.Count != 0) {
				SpecijalnaOprema so = dizajner.trupKomponente.specijalnaOprema[lstvNDspecOprema.SelectedIndices[0]];
				specijalnaOpremaZaOpis = so;
			}

			osvjeziNDstatistike();
			prebaciNDopis(InfoStranice.Pokretljivost);
		}

		private void cbNDprimMisija_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (prethodnaNDprimMisija == cbNDprimMisija.SelectedIndex)
				return;

			if (dizajner.dizajnPrimMisija != null) {
				TagTekst<Oruzje> tagTekst = (TagTekst<Oruzje>)cbNDprimMisija.Items[prethodnaNDprimMisija];
				cbNDprimMisija.Items[prethodnaNDprimMisija] = new TagTekst<Oruzje>(tagTekst.tag, tagTekst.tag.naziv);
			}
			prethodnaNDprimMisija = cbNDprimMisija.SelectedIndex;

			Oruzje misija = izvadiTag<Oruzje>(cbNDprimMisija);
			dizajner.dizajnPrimMisija = misija;
			osvjeziNDstatistike();
			prebaciNDopis(InfoStranice.PrimarnaMisija);
		}

		private void cbNDsekMisija_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (prethodnaNDsekMisija == cbNDsekMisija.SelectedIndex)
				return;

			if (dizajner.dizajnSekMisija != null) {
				TagTekst<Oruzje> tagTekst = (TagTekst<Oruzje>)cbNDsekMisija.Items[prethodnaNDsekMisija];
				cbNDsekMisija.Items[prethodnaNDsekMisija] = new TagTekst<Oruzje>(tagTekst.tag, tagTekst.tag.naziv);
			}
			prethodnaNDsekMisija = cbNDsekMisija.SelectedIndex;

			Oruzje misija = izvadiTag<Oruzje>(cbNDsekMisija);
			dizajner.dizajnSekMisija = misija;
			osvjeziNDstatistike();
			prebaciNDopis(InfoStranice.SekundarnaMisija);
		}

		private void chNDMZpogon_CheckedChanged(object sender, EventArgs e)
		{
			dizajner.dizajnMZPogon = chNDMZpogon.Checked;
			osvjeziNDstatistike();
			prebaciNDopis(InfoStranice.MZPogon);
		}

		private void hscrUdioMisija_ValueChanged(object sender, EventArgs e)
		{
			lblNDudioMisija.Text = hscrUdioMisija.Value + "%";
			dizajner.dizajnUdioPrimMisije = (100.0 - hscrUdioMisija.Value) / 100.0;
			osvjeziNDstatistike();

			if (prethodniNDinfo == InfoStranice.PrimarnaMisija)
				prebaciNDopis(InfoStranice.PrimarnaMisija);
			else
				prebaciNDopis(InfoStranice.SekundarnaMisija);
		}

		private void cbNDstit_SelectedIndexChanged(object sender, EventArgs e)
		{
			int stitIndeks = izvadiTag<int>(cbNDstit);
			if (stitIndeks < 0)
				dizajner.dizajnStit = null;
			else
				dizajner.dizajnStit = dizajner.komponente[dizajner.odabranTrup].stitovi[stitIndeks];

			osvjeziNDstatistike();
			prebaciNDopis(InfoStranice.Stit);
		}

		private void cbNDtaktika_SelectedIndexChanged(object sender, EventArgs e)
		{
			dizajner.dizajnTaktika = izvadiTag<Taktika>(cbNDtaktika);
			osvjeziNDstatistike();
			prebaciNDopis(InfoStranice.Taktika);
		}

		private void btnNDspecOpremaPlus_Click(object sender, EventArgs e)
		{
			if (lstvNDspecOprema.SelectedItems.Count == 0)
				return;

			promjeniNDspecOpremu(true, lstvNDspecOprema.SelectedIndices[0]);
		}

		private void btnNDspecOpremaMinus_Click(object sender, EventArgs e)
		{
			if (lstvNDspecOprema.SelectedItems.Count == 0)
				return;

			promjeniNDspecOpremu(false, lstvNDspecOprema.SelectedIndices[0]);
		}

		private void promjeniNDspecOpremu(bool dodaj, int soIndeks)
		{
			SpecijalnaOprema so = dizajner.trupKomponente.specijalnaOprema[soIndeks];
			int n;

			if (dodaj)
				n = dizajner.dodajSpecOpremu(so);
			else
				n = dizajner.makniSpecOpremu(so);

			if (n > 0)
				lstvNDspecOprema.Items[soIndeks].SubItems[0].Text = n + "x";
			else
				lstvNDspecOprema.Items[soIndeks].SubItems[0].Text = "";

			specijalnaOpremaZaOpis = so;

			osvjeziNDstatistike();
			prebaciNDopis(InfoStranice.SpecijalnaOprema);
		}

		private void cbNDinfoStrana_SelectedIndexChanged(object sender, EventArgs e)
		{
			InfoStranice stranica = ((TagTekst<InfoStranice>)cbNDinfoStrana.SelectedItem).tag;
			ispisiOpis(stranica, dizajner.dizajn);
		}

		private void txtNDnaziv_TextChanged(object sender, EventArgs e)
		{
			dizajner.dizajnIme = txtNDnaziv.Text;
			provjeriDizajn();
		}

		private void btnNDinfoPrethodna_Click(object sender, EventArgs e)
		{
			if (cbNDinfoStrana.SelectedIndex <= 0)
				cbNDinfoStrana.SelectedIndex = cbNDinfoStrana.Items.Count - 1;
			else
				cbNDinfoStrana.SelectedIndex--;
		}

		private void btnNDinfoSlijedeca_Click(object sender, EventArgs e)
		{
			if (cbNDinfoStrana.SelectedIndex + 1 >= cbNDinfoStrana.Items.Count)
				cbNDinfoStrana.SelectedIndex = 0;
			else
				cbNDinfoStrana.SelectedIndex++;
		}

		private void lstvNDspecOprema_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (lstvNDspecOprema.SelectedItems.Count == 0)
				return;

			SpecijalnaOprema so = dizajner.trupKomponente.specijalnaOprema[lstvNDspecOprema.SelectedIndices[0]];
			specijalnaOpremaZaOpis = so;
			prebaciNDopis(InfoStranice.SpecijalnaOprema);
		}

		private void lstvNDspecOprema_ItemActivate(object sender, EventArgs e)
		{
			if (lstvNDspecOprema.SelectedItems.Count == 0)
				return;

			promjeniNDspecOpremu(true, lstvNDspecOprema.SelectedIndices[0]);
		}

		private void btnSpremi_Click(object sender, EventArgs e)
		{
			HashSet<Sazetak> postojeciDizajnovi = new HashSet<Sazetak>();
			foreach (DizajnZgrada dizajnZgrada in igrac.dizajnoviBrodova)
				postojeciDizajnovi.Add(dizajnZgrada.dizajn.stil);

			if (postojeciDizajnovi.Contains(dizajner.dizajn.stil)) {
				MessageBox.Show("Dizajn istih karakteristika već postoji", "Postojeći dizajn", MessageBoxButtons.OK, MessageBoxIcon.Stop);
				return;
			}

			igrac.dodajDizajn(dizajner.dizajn);
			dodajDizajn(dizajner.dizajn);
			tabCtrlFlote.SelectedTab = tabDizajnovi;

			dizajner.reset(igrac);
			cbNDvelicina.SelectedItem = cbNDvelicina.SelectedItem;
			cbNDvelicina.SelectedIndex = cbNDvelicina.SelectedIndex;
			cbNDprimMisija.SelectedIndex = cbNDprimMisija.SelectedIndex;
			cbNDsekMisija.SelectedIndex = cbNDsekMisija.SelectedIndex;
			cbNDstit.SelectedIndex = cbNDstit.SelectedIndex;
			cbNDtaktika.SelectedIndex = cbNDtaktika.SelectedIndex;
			hscrUdioMisija.Value = hscrUdioMisija.Value;
			txtNDnaziv.Text = "";
		}

		private void bntNDZadrziInfo_Click(object sender, EventArgs e)
		{
			if (zadrziNDInfo) {
				zadrziNDInfo = false;
				btnNDZadrziInfo.Text = "";
			}
			else {
				zadrziNDInfo = true;
				btnNDZadrziInfo.Text = "*";
			}
		}
		#endregion

		private void dodajDizajn(Dizajn dizajn)
		{
			ListViewItem item = new ListViewItem(dizajn.ime);
			item.SubItems.Add(Fje.PrefiksFormater(dizajn.brojBrodova));
			item.Tag = dizajn;

			lstvDizajnovi.SmallImageList.Images.Add(dizajn.ikona);
			item.ImageIndex = lstvDizajnovi.SmallImageList.Images.Count - 1;

			lstvDizajnovi.Items.Add(item);
		}

		private void lstvDizajnovi_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (lstvDizajnovi.SelectedItems.Count == 0)
				return;
			Dizajn dizajn = (Dizajn)lstvDizajnovi.SelectedItems[0].Tag;

			picSlikaDizajna.Image = dizajn.slika;

			List<string> opisDizajna = new List<string>();
			opisDizajna.Add(dizajn.ime);
			opisDizajna.Add(dizajn.trup.naziv);
			opisDizajna.Add("");

			foreach (InfoStranice stranica in Enum.GetValues(typeof(InfoStranice))) {
				if (stranica == InfoStranice.SpecijalnaOprema)
					foreach (SpecijalnaOprema so in dizajn.specijalnaOprema.Keys) {
						specijalnaOpremaZaOpis = so;
						opisDizajna.AddRange(opis(stranica, dizajn, false));
						opisDizajna.Add("");
					}
				else {
					opisDizajna.AddRange(opis(stranica, dizajn, false));
					opisDizajna.Add("");
				}

				if (stranica == InfoStranice.SekundarnaMisija) {
					opisDizajna.Add("Oklop:");
					opisDizajna.Add("");
					opisDizajna.Add(dizajn.oklop.naziv);
					opisDizajna.Add("Izdržljivost: " + dizajn.izdrzljivostOklopa);
					opisDizajna.Add("");
				}
			}

			txtDizajnInfo.Lines = opisDizajna.ToArray();

			if (dizajn.brojBrodova == 0) btnUkloniDizajn.Enabled = true;
			else btnUkloniDizajn.Enabled = false;
		}

		private void btnUkloniDizajn_Click(object sender, EventArgs e)
		{
			if (lstvDizajnovi.SelectedItems.Count == 0)
				return;
			int indeks = lstvDizajnovi.SelectedIndices[0];

			if (igrac.dizajnoviBrodova[indeks].dizajn.brojBrodova == 0) {
				DizajnZgrada dizajnZgrada = igrac.dizajnoviBrodova[indeks];
				if (!igrac.dizajnoviUGradnji().Contains(dizajnZgrada.dizajn)) {
					igrac.dizajnoviBrodova.RemoveAt(indeks);
					lstvDizajnovi.Items.RemoveAt(indeks);
				}
			}
		}


	}
}
