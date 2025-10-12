export function formatCurrency(amount, currency = 'TRY', locale = undefined) {
  const num = Number(amount || 0);
  const curr = typeof currency === 'string' && currency ? currency : 'TRY';
  try {
    // Türk Lirası için para tutarı önce, TL işareti sonra gelsin
    if (curr === 'TRY') {
      // 'tr-TR' locale'da TL işareti sonda olur, ama locale farklıysa elle yap
      const formatted = new Intl.NumberFormat('tr-TR', {
        style: 'decimal',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      }).format(num);
      return `${formatted} ₺`;
    } else {
      return new Intl.NumberFormat(locale, {
        style: 'currency',
        currency: curr,
        maximumFractionDigits: 2,
      }).format(num);
    }
  } catch (e) {
    return new Intl.NumberFormat(locale, {
      style: 'currency',
      currency: 'TRY',
      maximumFractionDigits: 2,
    }).format(num);
  }
}


