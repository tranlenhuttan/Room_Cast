/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './src/RoomCast/Views/**/*.cshtml',
    './src/RoomCast/Pages/**/*.cshtml',
    './src/RoomCast/wwwroot/js/**/*.js'
  ],
  theme: {
    extend: {},
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('@tailwindcss/typography')
  ],
};
