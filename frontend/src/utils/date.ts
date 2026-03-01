const resolvedTimeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;

export const formatDateTimeInUserTimeZone = (value?: string | null): string => {
  if (!value) return '-';

  return new Intl.DateTimeFormat('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    timeZone: resolvedTimeZone,
  }).format(new Date(value));
};

export const formatExtractedDateTime = (value?: string | null): string => {
  if (!value) return '-';

  const match = value.match(/^(\d{4})-(\d{2})-(\d{2})(?:[T\s](\d{2}):(\d{2}))?/);
  if (match) {
    const [, year, month, day, hour = '00', minute = '00'] = match;
    return `${day}/${month}/${year} ${hour}:${minute}`;
  }

  return formatDateTimeInUserTimeZone(value);
};
